using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class EventTypeRepository(DbConnectionFactory db, ErrorLogRepository errorLog) : IEventTypeRepository
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
                 ORDER BY title
                """);
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
                 WHERE event_type_id = @id
                """,
                new { id });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventTypeRepository));
            throw;
        }
    }

    public async Task<EventType> CreateAsync(EventType type)
    {
        try
        {
            using var conn = db.Create();
            type.EventTypeId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO event_type (title, description)
                VALUES (@Title, @Description)
                RETURNING event_type_id
                """,
                type);
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
                 WHERE event_type_id = @EventTypeId
                """,
                type);
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
                 WHERE event_type_id = @id
                """,
                new { id });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventTypeRepository));
            throw;
        }
    }
}
