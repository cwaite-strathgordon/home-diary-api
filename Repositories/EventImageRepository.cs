using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class EventImageRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext)
    : IEventImageRepository
{
    private const string MetadataColumns =
        "event_image_id, event_id, file_name, content_type, file_size, created_date, created_by_id";

    public async Task<bool> EventExistsAsync(int eventId)
    {
        using var conn = db.Create();
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM home_event WHERE event_id = @eventId AND client_id=@clientId)",
            new { eventId, clientId = clientContext.RequireClientId() });
    }

    public async Task<IEnumerable<EventImage>> GetForEventAsync(int eventId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventImage>(
                $"SELECT {MetadataColumns} FROM event_image WHERE event_id = @eventId AND client_id=@clientId ORDER BY created_date DESC",
                new { eventId, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventImageRepository));
            throw;
        }
    }

    public async Task<EventImage?> GetFileAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<EventImage>(
                $"SELECT {MetadataColumns}, image_data FROM event_image WHERE event_image_id = @id AND client_id=@clientId",
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventImageRepository));
            throw;
        }
    }

    public async Task<EventImage> CreateAsync(EventImage image)
    {
        try
        {
            using var conn = db.Create();
            image.EventImageId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO event_image
                       (client_id, event_id, file_name, content_type, file_size, image_data, created_by_id)
                VALUES (@clientId, @EventId, @FileName, @ContentType, @FileSize, @ImageData, @CreatedById)
                RETURNING event_image_id
                """, new { clientId = clientContext.RequireClientId(), image.EventId, image.FileName,
                            image.ContentType, image.FileSize, image.ImageData, image.CreatedById });
            image.CreatedDate = DateTimeOffset.UtcNow;
            image.ImageData = [];
            return image;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventImageRepository));
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync(
                "DELETE FROM event_image WHERE event_image_id = @id AND client_id=@clientId",
                new { id, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventImageRepository));
            throw;
        }
    }
}
