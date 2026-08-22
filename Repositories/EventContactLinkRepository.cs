using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class EventContactLinkRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : IEventContactLinkRepository
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
                 WHERE event_id = @eventId AND client_id=@clientId
                 ORDER BY contact_id
                """,
                new { eventId, clientId = clientContext.RequireClientId() });
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
                 WHERE contact_id = @contactId AND client_id=@clientId
                 ORDER BY event_id
                """,
                new { contactId, clientId = clientContext.RequireClientId() });
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
                INSERT INTO event_contact_link (client_id, contact_id, event_id)
                SELECT @clientId, @ContactId, @EventId
                 WHERE EXISTS (SELECT 1 FROM contact WHERE contact_id=@ContactId AND client_id=@clientId)
                   AND EXISTS (SELECT 1 FROM home_event WHERE event_id=@EventId AND client_id=@clientId)
                ON CONFLICT DO NOTHING
                """,
                new { clientId = clientContext.RequireClientId(), link.ContactId, link.EventId });
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
                   AND client_id  = @clientId
                """,
                new { contactId, eventId, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventContactLinkRepository));
            throw;
        }
    }
}
