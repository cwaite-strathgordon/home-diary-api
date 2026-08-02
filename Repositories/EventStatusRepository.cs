using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class EventStatusRepository(DbConnectionFactory db, ErrorLogRepository errorLog) : IEventStatusRepository
{
    public async Task<IEnumerable<EventStatus>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventStatus>(
                """
                SELECT event_status_id,
                       title,
                       description
                  FROM event_status
                 ORDER BY title
                """);
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventStatusRepository));
            throw;
        }
    }

    public async Task<EventStatus?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<EventStatus>(
                """
                SELECT event_status_id,
                       title,
                       description
                  FROM event_status
                 WHERE event_status_id = @id
                """,
                new { id });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventStatusRepository));
            throw;
        }
    }

    public async Task<EventStatus> CreateAsync(EventStatus status)
    {
        try
        {
            using var conn = db.Create();
            status.EventStatusId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO event_status (title, description)
                VALUES (@Title, @Description)
                RETURNING event_status_id
                """,
                status);
            return status;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventStatusRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(EventStatus status)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE event_status
                   SET title       = @Title,
                       description = @Description
                 WHERE event_status_id = @EventStatusId
                """,
                status);
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventStatusRepository));
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
                DELETE FROM event_status
                 WHERE event_status_id = @id
                """,
                new { id });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventStatusRepository));
            throw;
        }
    }
}
