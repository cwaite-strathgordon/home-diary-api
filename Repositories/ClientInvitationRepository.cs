using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public sealed class ClientInvitationRepository(DbConnectionFactory db, ClientContext clientContext)
    : IClientInvitationRepository
{
    public async Task<IEnumerable<ClientInvitation>> GetAllAsync()
    {
        using var conn = db.Create();
        return await conn.QueryAsync<ClientInvitation>(
            """
            SELECT client_invitation_id, email, admin, invitation_token, expires_at, accepted_at,
                   sent_at, ses_message_id, delivery_error
              FROM client_invitation WHERE client_id=@clientId
             ORDER BY created_at DESC
            """, new { clientId = clientContext.RequireClientId() });
    }

    public async Task<ClientInvitation> CreateAsync(string email, bool admin, int invitedById)
    {
        using var conn = db.Create();
        return await conn.QuerySingleAsync<ClientInvitation>(
            """
            INSERT INTO client_invitation(client_id, email, admin, invited_by_id)
            VALUES (@clientId, @email, @admin, @invitedById)
            ON CONFLICT (client_id, lower(email)) WHERE accepted_at IS NULL
            DO UPDATE SET admin=EXCLUDED.admin, invited_by_id=EXCLUDED.invited_by_id,
                          invitation_token=gen_random_uuid(), expires_at=now()+interval '7 days',
                          sent_at=NULL, ses_message_id=NULL, delivery_error=NULL
            RETURNING client_invitation_id, email, admin, invitation_token, expires_at, accepted_at,
                      sent_at, ses_message_id, delivery_error
            """, new { clientId = clientContext.RequireClientId(), email = email.Trim(), admin, invitedById });
    }

    public async Task MarkSentAsync(long id, string sesMessageId)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(
            """
            UPDATE client_invitation SET sent_at=now(), ses_message_id=@sesMessageId,
                   delivery_error=NULL
             WHERE client_invitation_id=@id AND client_id=@clientId AND accepted_at IS NULL
            """, new { id, sesMessageId, clientId = clientContext.RequireClientId() });
    }

    public async Task MarkDeliveryFailedAsync(long id, string error)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(
            """
            UPDATE client_invitation SET delivery_error=@error, sent_at=NULL, ses_message_id=NULL
             WHERE client_invitation_id=@id AND client_id=@clientId AND accepted_at IS NULL
            """, new { id, error = error.Length > 2000 ? error[..2000] : error,
                       clientId = clientContext.RequireClientId() });
    }

    public async Task<bool> RevokeAsync(long id)
    {
        using var conn = db.Create();
        return await conn.ExecuteAsync(
            "DELETE FROM client_invitation WHERE client_invitation_id=@id AND client_id=@clientId AND accepted_at IS NULL",
            new { id, clientId = clientContext.RequireClientId() }) > 0;
    }
}
