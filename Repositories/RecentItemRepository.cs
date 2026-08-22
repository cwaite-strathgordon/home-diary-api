using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public sealed class RecentItemRepository(
    DbConnectionFactory db,
    ErrorLogRepository errorLog,
    ClientContext clientContext) : IRecentItemRepository
{
    private const int DefaultLimit = 20;

    public async Task<IEnumerable<RecentItem>> GetAsync(int userId)
    {
        try
        {
            using var conn = db.Create();
            var clientId = clientContext.RequireClientId();
            var limit = await GetLimitAsync(conn, clientId);
            return await conn.QueryAsync<RecentItem>(
                """
                SELECT recent_item_view_id, item_type, item_id, title, viewed_at
                  FROM recent_item_view
                 WHERE client_id = @clientId AND user_id = @userId
                 ORDER BY viewed_at DESC, recent_item_view_id DESC
                 LIMIT @limit
                """, new { clientId, userId, limit });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(RecentItemRepository));
            throw;
        }
    }

    public async Task<RecentItem?> RecordAsync(string itemType, int itemId, int userId)
    {
        try
        {
            itemType = itemType.Trim().ToLowerInvariant();
            if (itemType is not ("task" or "project" or "contact"))
                throw new ArgumentException("Recent item type must be task, project, or contact.");

            using var conn = db.Create();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var clientId = clientContext.RequireClientId();
            var title = await ResolveTitleAsync(conn, transaction, clientId, itemType, itemId);
            if (title is null) return null;

            var item = await conn.QuerySingleAsync<RecentItem>(
                """
                INSERT INTO recent_item_view (client_id, user_id, item_type, item_id, title)
                VALUES (@clientId, @userId, @itemType, @itemId, @title)
                ON CONFLICT (client_id, user_id, item_type, item_id) DO UPDATE
                   SET title = EXCLUDED.title,
                       viewed_at = now()
                RETURNING recent_item_view_id, item_type, item_id, title, viewed_at
                """, new { clientId, userId, itemType, itemId, title }, transaction);

            var limit = await GetLimitAsync(conn, clientId, transaction);
            await conn.ExecuteAsync(
                """
                DELETE FROM recent_item_view
                 WHERE recent_item_view_id IN (
                       SELECT recent_item_view_id
                         FROM recent_item_view
                        WHERE client_id = @clientId AND user_id = @userId
                        ORDER BY viewed_at DESC, recent_item_view_id DESC
                       OFFSET @limit)
                """, new { clientId, userId, limit }, transaction);

            transaction.Commit();
            return item;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(RecentItemRepository));
            throw;
        }
    }

    private static async Task<string?> ResolveTitleAsync(
        System.Data.IDbConnection conn,
        System.Data.IDbTransaction transaction,
        int clientId,
        string itemType,
        int itemId)
    {
        var sql = itemType switch
        {
            "task" => "SELECT title FROM home_event WHERE client_id=@clientId AND event_id=@itemId",
            "project" => "SELECT title FROM project WHERE client_id=@clientId AND project_id=@itemId",
            "contact" => """
                SELECT COALESCE(NULLIF(trim(concat_ws(' ', first_name, last_name)), ''),
                               NULLIF(trim(company_name), ''), 'Unnamed contact')
                  FROM contact
                 WHERE client_id=@clientId AND contact_id=@itemId
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(itemType))
        };
        return await conn.QuerySingleOrDefaultAsync<string>(sql, new { clientId, itemId }, transaction);
    }

    private static async Task<int> GetLimitAsync(
        System.Data.IDbConnection conn,
        int clientId,
        System.Data.IDbTransaction? transaction = null)
    {
        var raw = await conn.QuerySingleOrDefaultAsync<string>(
            """
            SELECT parameter_value
              FROM application_parameter
             WHERE client_id=@clientId AND parameter_key='recent_items.limit'
            """, new { clientId }, transaction);
        return int.TryParse(raw, out var value) ? Math.Clamp(value, 1, 100) : DefaultLimit;
    }
}
