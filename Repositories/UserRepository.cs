using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class UserRepository(DbConnectionFactory db, ErrorLogRepository errorLog) : IUserRepository
{
    private const string SelectColumns =
        """
        SELECT user_id,
               first_name,
               last_name,
               email,
               admin,
               mobile_number,
               oauth_provider,
               oauth_id,
               oauth_email
          FROM app_user
        """;

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<User>(
                $"""
                {SelectColumns}
                 ORDER BY last_name, first_name
                """);
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
                 WHERE user_id = @id
                """,
                new { id });
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
                 WHERE LOWER(email) = LOWER(@email)
                """,
                new { email });
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
                 WHERE oauth_provider = @provider
                   AND oauth_id       = @oauthId
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
                       (first_name, last_name, email, admin, mobile_number,
                        oauth_provider, oauth_id, oauth_email)
                VALUES (@FirstName, @LastName, @Email, @Admin, @MobileNumber,
                        @OAuthProvider, @OAuthId, @OAuthEmail)
                RETURNING user_id
                """,
                user);
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
                       mobile_number = @MobileNumber
                 WHERE user_id = @UserId
                """,
                user);
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
                 WHERE user_id = @id
                """,
                new { id });
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
                 WHERE user_id = @userId
                """,
                new { userId, provider, oauthId, oauthEmail });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }

    public async Task<User> UpsertFromOAuthAsync(
        string provider, string oauthId, string email,
        string? firstName, string? lastName)
    {
        try
        {
            using var conn = db.Create();

            // Find by OAuth identity first, then fall back to email match
            var existing = await conn.QuerySingleOrDefaultAsync<User>(
                $"""
                {SelectColumns}
                 WHERE (oauth_provider = @provider AND oauth_id = @oauthId)
                    OR  LOWER(email) = LOWER(@email)
                 LIMIT 1
                """,
                new { provider, oauthId, email });

            if (existing is not null)
            {
                await conn.ExecuteAsync(
                    """
                    UPDATE app_user
                       SET oauth_provider = @provider,
                           oauth_id       = @oauthId,
                           oauth_email    = @email
                     WHERE user_id = @UserId
                    """,
                    new { provider, oauthId, email, existing.UserId });

                existing.OAuthProvider = provider;
                existing.OAuthId       = oauthId;
                existing.OAuthEmail    = email;
                return existing;
            }

            var newUser = new User
            {
                FirstName     = firstName,
                LastName      = lastName,
                Email         = email,
                Admin         = false,
                OAuthProvider = provider,
                OAuthId       = oauthId,
                OAuthEmail    = email
            };
            return await CreateAsync(newUser);
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(UserRepository));
            throw;
        }
    }
}
