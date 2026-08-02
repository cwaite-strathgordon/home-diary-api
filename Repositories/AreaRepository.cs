using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class AreaRepository(DbConnectionFactory db, ErrorLogRepository errorLog) : IAreaRepository
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
                 ORDER BY title
                """);
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
                 WHERE area_id = @id
                """,
                new { id });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(AreaRepository));
            throw;
        }
    }

    public async Task<Area> CreateAsync(Area area)
    {
        try
        {
            using var conn = db.Create();
            area.AreaId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO area (title, description)
                VALUES (@Title, @Description)
                RETURNING area_id
                """,
                area);
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
                 WHERE area_id = @AreaId
                """,
                area);
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
                 WHERE area_id = @id
                """,
                new { id });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(AreaRepository));
            throw;
        }
    }
}
