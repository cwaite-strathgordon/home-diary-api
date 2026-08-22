using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class EventTypeRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : IEventTypeRepository
{
    public async Task<IEnumerable<EventType>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventType>(
                """
                SELECT event_type_id,
                       title,
                       description
                  FROM event_type
                 WHERE client_id = @clientId
                 ORDER BY title
                """, new { clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventTypeRepository));
            throw;
        }
    }

    public async Task<EventType?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<EventType>(
                """
                SELECT event_type_id,
                       title,
                       description
                  FROM event_type
                 WHERE event_type_id = @id AND client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventTypeRepository));
            throw;
        }
    }

    public async Task<bool> TitleExistsAsync(string title, int? excludingId = null)
    {
        using var conn = db.Create();
        return await conn.ExecuteScalarAsync<bool>(
            excludingId.HasValue
                ? "SELECT EXISTS (SELECT 1 FROM event_type WHERE client_id = @clientId AND LOWER(title) = LOWER(@title) AND event_type_id <> @excludingId)"
                : "SELECT EXISTS (SELECT 1 FROM event_type WHERE client_id = @clientId AND LOWER(title) = LOWER(@title))",
            new { title, excludingId, clientId = clientContext.RequireClientId() });
    }

    public async Task<bool> IsInUseAsync(int id)
    {
        using var conn = db.Create();
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM home_event WHERE event_type_id = @id AND client_id = @clientId)",
            new { id, clientId = clientContext.RequireClientId() });
    }

    public async Task<EventType> CreateAsync(EventType type)
    {
        try
        {
            using var conn = db.Create();
            type.EventTypeId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO event_type (client_id, title, description)
                VALUES (@clientId, @Title, @Description)
                RETURNING event_type_id
                """,
                new { clientId = clientContext.RequireClientId(), type.Title, type.Description });
            return type;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventTypeRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(EventType type)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE event_type
                   SET title       = @Title,
                       description = @Description
                 WHERE event_type_id = @EventTypeId AND client_id = @clientId
                """,
                new { type.EventTypeId, type.Title, type.Description, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventTypeRepository));
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                DELETE FROM event_type
                 WHERE event_type_id = @id AND client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventTypeRepository));
            throw;
        }
    }
}
