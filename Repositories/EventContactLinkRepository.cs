using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class EventContactLinkRepository(DbConnectionFactory db, ErrorLogRepository errorLog) : IEventContactLinkRepository
{
    public async Task<IEnumerable<EventContactLink>> GetByEventIdAsync(int eventId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventContactLink>(
                """
                SELECT contact_id,
                       event_id
                  FROM event_contact_link
                 WHERE event_id = @eventId
                 ORDER BY contact_id
                """,
                new { eventId });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventContactLinkRepository));
            throw;
        }
    }

    public async Task<IEnumerable<EventContactLink>> GetByContactIdAsync(int contactId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventContactLink>(
                """
                SELECT contact_id,
                       event_id
                  FROM event_contact_link
                 WHERE contact_id = @contactId
                 ORDER BY event_id
                """,
                new { contactId });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventContactLinkRepository));
            throw;
        }
    }

    public async Task<bool> CreateAsync(EventContactLink link)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                INSERT INTO event_contact_link (contact_id, event_id)
                VALUES (@ContactId, @EventId)
                ON CONFLICT DO NOTHING
                """,
                link);
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventContactLinkRepository));
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int contactId, int eventId)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                DELETE FROM event_contact_link
                 WHERE contact_id = @contactId
                   AND event_id   = @eventId
                """,
                new { contactId, eventId });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventContactLinkRepository));
            throw;
        }
    }
}
