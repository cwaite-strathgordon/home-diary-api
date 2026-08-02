using Dapper;
using HomeDiary_api.Data;

namespace HomeDiary_api.Repositories;

public class ErrorLogRepository(DbConnectionFactory db)
{
    public async Task LogAsync(string message, string? stackTrace, string? source)
    {
        try
        {
            using var conn = db.Create();
            await conn.ExecuteAsync(
                """
                INSERT INTO error_log (error_message, stack_trace, source, created_date)
                VALUES (@message, @stackTrace, @source, NOW())
                """,
                new { message, stackTrace, source });
        }
        catch
        {
            // Swallow logging failures so they never mask the original exception
        }
    }
}
