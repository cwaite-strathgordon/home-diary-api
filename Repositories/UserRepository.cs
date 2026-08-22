using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class UserRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : IUserRepository
{
    private const string SelectColumns =
        """
        SELECT u.user_id,
               u.client_id,
               c.name AS client_name,
               c.inbound_email_address,
               first_name,
               last_name,
               email,
               admin,
               disabled,
               mobile_number,
               oauth_provider,
               oauth_id,
               oauth_email,
               u.last_login_at
          FROM app_user u
          LEFT JOIN client c ON c.client_id = u.client_id
        """;

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<User>(
                $"""
                {SelectColumns}
                 WHERE u.client_id = @clientId
                 ORDER BY last_name, first_name
                """, new { clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<User>(
                $"""
                {SelectColumns}
                 WHERE u.user_id = @id
                   AND u.client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<User>(
                $"""
                {SelectColumns}
                 WHERE LOWER(u.email) = LOWER(@email)
                   AND u.client_id = @clientId
                """,
                new { email, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<User?> GetByOAuthAsync(string provider, string oauthId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<User>(
                $"""
                {SelectColumns}
                 WHERE u.oauth_provider = @provider
                   AND u.oauth_id       = @oauthId
                """,
                new { provider, oauthId });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<User> CreateAsync(User user)
    {
        try
        {
            using var conn = db.Create();
            user.UserId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO app_user
                       (client_id, first_name, last_name, email, admin, mobile_number,
                        oauth_provider, oauth_id, oauth_email)
                VALUES (@ClientId, @FirstName, @LastName, @Email, @Admin, @MobileNumber,
                        @OAuthProvider, @OAuthId, @OAuthEmail)
                RETURNING user_id
                """,
                new { ClientId = clientContext.RequireClientId(), user.FirstName, user.LastName,
                      user.Email, user.Admin, user.MobileNumber, user.OAuthProvider, user.OAuthId, user.OAuthEmail });
            return user;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(User user)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE app_user
                   SET first_name    = @FirstName,
                       last_name     = @LastName,
                       email         = @Email,
                       admin         = @Admin,
                       disabled      = @Disabled,
                       mobile_number = @MobileNumber,
                       updated_at    = NOW()
                 WHERE user_id = @UserId
                   AND client_id = @ClientId
                """,
                new { user.UserId, ClientId = clientContext.RequireClientId(), user.FirstName,
                      user.LastName, user.Email, user.Admin, user.Disabled, user.MobileNumber });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
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
                DELETE FROM app_user
                 WHERE user_id = @id AND client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<bool> LinkOAuthAsync(int userId, string provider, string oauthId, string? oauthEmail)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE app_user
                   SET oauth_provider = @provider,
                       oauth_id       = @oauthId,
                       oauth_email    = @oauthEmail
                 WHERE user_id = @userId AND client_id = @clientId
                """,
                new { userId, provider, oauthId, oauthEmail, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<User> UpsertFromOAuthAsync(
        string provider, string oauthId, string? email,
        string? firstName, string? lastName, bool emailVerified, Guid? invitationToken)
    {
        try
        {
            using var conn = db.Create();
            var user = await conn.QuerySingleOrDefaultAsync<User>(
                """
                INSERT INTO app_user
                       (first_name, last_name, email, admin,
                        oauth_provider, oauth_id, oauth_email, last_login_at)
                VALUES (@firstName, @lastName, @email, false,
                        @provider, @oauthId, @email, NOW())
                ON CONFLICT (oauth_provider, oauth_id)
                DO UPDATE SET
                    oauth_email = COALESCE(NULLIF(EXCLUDED.oauth_email, ''), app_user.oauth_email),
                    updated_at  = NOW(),
                    last_login_at = NOW()
                WHERE app_user.disabled = false
                RETURNING user_id,
                          client_id,
                          first_name,
                          last_name,
                          email,
                          admin,
                          disabled,
                          mobile_number,
                          oauth_provider,
                          oauth_id,
                          oauth_email,
                          last_login_at
                """,
                new { provider, oauthId, email, firstName, lastName });

            // A disabled user is deliberately excluded from the upsert update.
            // Return it so the controller can issue a clear 403 response.
            user ??= await conn.QuerySingleAsync<User>(
                $"""
                {SelectColumns}
                 WHERE u.oauth_provider = @provider AND u.oauth_id = @oauthId
                """,
                new { provider, oauthId });

            // A client administrator can pre-authorise another email address.
            // The validated Auth0 email claims the newest live invitation.
            if (user.ClientId is null && emailVerified && invitationToken.HasValue &&
                !string.IsNullOrWhiteSpace(email) && !user.Disabled)
            {
                var invitation = await conn.QuerySingleOrDefaultAsync<InvitationAcceptance>(
                    """
                    WITH claimed AS
                    (
                        UPDATE client_invitation
                           SET accepted_at=now()
                         WHERE invitation_token=@invitationToken
                           AND lower(email)=lower(@email)
                           AND accepted_at IS NULL AND expires_at > now()
                           AND EXISTS (SELECT 1 FROM app_user u
                                        WHERE u.user_id=@userId AND u.client_id IS NULL)
                        RETURNING client_id, admin
                    )
                    UPDATE app_user u
                       SET client_id=claimed.client_id, admin=claimed.admin, updated_at=now()
                      FROM claimed
                     WHERE u.user_id=@userId AND u.client_id IS NULL
                    RETURNING claimed.client_id, claimed.admin
                    """, new { email, invitationToken, userId = user.UserId });
                if (invitation is not null)
                {
                    user.ClientId = invitation.ClientId;
                    user.Admin = invitation.Admin;
                    user.ClientName = await conn.QuerySingleAsync<string>(
                        "SELECT name FROM client WHERE client_id=@ClientId", invitation);
                }
            }
            return user;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    private sealed class InvitationAcceptance
    {
        public int ClientId { get; set; }
        public bool Admin { get; set; }
    }
}
