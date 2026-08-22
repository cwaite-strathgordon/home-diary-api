using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class EventPriorityRepository(DbConnectionFactory db, ErrorLogRepository errorLog)
    : IEventPriorityRepository
{
    public async Task<IEnumerable<EventPriority>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventPriority>(
                """
                SELECT event_priority_id,
                       title,
                       description
                  FROM event_priority
                 ORDER BY event_priority_id
                """);
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventPriorityRepository));
            throw;
        }
    }

    public async Task<EventPriority?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<EventPriority>(
                """
                SELECT event_priority_id,
                       title,
                       description
                  FROM event_priority
                 WHERE event_priority_id = @id
                """,
                new { id });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventPriorityRepository));
            throw;
        }
    }
}
