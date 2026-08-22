using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class AreaRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : IAreaRepository
{
    public async Task<IEnumerable<Area>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<Area>(
                """
                SELECT area_id,
                       title,
                       description
                  FROM area
                 WHERE client_id = @clientId
                 ORDER BY title
                """, new { clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(AreaRepository));
            throw;
        }
    }

    public async Task<Area?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<Area>(
                """
                SELECT area_id,
                       title,
                       description
                  FROM area
                 WHERE area_id = @id AND client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(AreaRepository));
            throw;
        }
    }

    public async Task<bool> TitleExistsAsync(string title, int? excludingId = null)
    {
        using var conn = db.Create();
        return await conn.ExecuteScalarAsync<bool>(
            excludingId.HasValue
                ? "SELECT EXISTS (SELECT 1 FROM area WHERE client_id = @clientId AND LOWER(title) = LOWER(@title) AND area_id <> @excludingId)"
                : "SELECT EXISTS (SELECT 1 FROM area WHERE client_id = @clientId AND LOWER(title) = LOWER(@title))",
            new { title, excludingId, clientId = clientContext.RequireClientId() });
    }

    public async Task<bool> IsInUseAsync(int id)
    {
        using var conn = db.Create();
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM home_event WHERE area_id = @id AND client_id = @clientId)",
            new { id, clientId = clientContext.RequireClientId() });
    }

    public async Task<Area> CreateAsync(Area area)
    {
        try
        {
            using var conn = db.Create();
            area.AreaId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO area (client_id, title, description)
                VALUES (@clientId, @Title, @Description)
                RETURNING area_id
                """,
                new { clientId = clientContext.RequireClientId(), area.Title, area.Description });
            return area;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(AreaRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Area area)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE area
                   SET title       = @Title,
                       description = @Description
                 WHERE area_id = @AreaId AND client_id = @clientId
                """,
                new { area.AreaId, area.Title, area.Description, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(AreaRepository));
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
                DELETE FROM area
                 WHERE area_id = @id AND client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(AreaRepository));
            throw;
        }
    }
}
