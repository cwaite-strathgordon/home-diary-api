using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class EventDocumentRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext)
    : IEventDocumentRepository
{
    private const string MetadataColumns =
        "event_document_id, event_id, file_name, content_type, file_size, created_date, created_by_id";

    private const string EnrichedMetadataColumns =
        "ed.event_document_id, ed.event_id, ed.file_name, ed.content_type, ed.file_size, " +
        "ed.created_date, ed.created_by_id, he.title AS event_title, " +
        "he.project_id, p.title AS project_title";

    public async Task<bool> EventExistsAsync(int eventId)
    {
        using var conn = db.Create();
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM home_event WHERE event_id = @eventId AND client_id=@clientId)",
            new { eventId, clientId = clientContext.RequireClientId() });
    }

    public async Task<IEnumerable<EventDocument>> GetForEventAsync(int eventId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventDocument>(
                $"SELECT {MetadataColumns} FROM event_document WHERE event_id = @eventId AND client_id=@clientId ORDER BY created_date DESC",
                new { eventId, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventDocumentRepository));
            throw;
        }
    }

    public async Task<IEnumerable<EventDocument>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventDocument>(
                $"""
                SELECT {EnrichedMetadataColumns}
                  FROM event_document ed
                  JOIN home_event he ON he.event_id = ed.event_id
                  LEFT JOIN project p ON p.project_id = he.project_id
                 WHERE ed.client_id = @clientId
                 ORDER BY p.title NULLS LAST, he.title, ed.created_date DESC
                """, new { clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventDocumentRepository));
            throw;
        }
    }

    public async Task<int> GetCountAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*)::int FROM event_document WHERE client_id=@clientId",
                new { clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventDocumentRepository));
            throw;
        }
    }

    public async Task<IEnumerable<EventDocument>> SearchAsync(string query, int? eventId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<EventDocument>(
                $"""
                WITH search AS (SELECT websearch_to_tsquery('english', @query) AS terms)
                SELECT {EnrichedMetadataColumns},
                       ts_headline('english', ed.extracted_text, search.terms,
                           'MaxWords=24, MinWords=8, StartSel=<mark>, StopSel=</mark>') AS search_snippet
                  FROM event_document ed
                  JOIN home_event he ON he.event_id = ed.event_id
                  LEFT JOIN project p ON p.project_id = he.project_id
                  CROSS JOIN search
                 WHERE ed.client_id = @clientId
                   AND (@eventId IS NULL OR ed.event_id = @eventId)
                   AND ed.search_vector @@ search.terms
                 ORDER BY ts_rank(ed.search_vector, search.terms) DESC, ed.created_date DESC
                """, new { query, eventId, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventDocumentRepository));
            throw;
        }
    }

    public async Task<EventDocument?> GetFileAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<EventDocument>(
                $"SELECT {MetadataColumns}, file_data FROM event_document WHERE event_document_id = @id AND client_id=@clientId",
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventDocumentRepository));
            throw;
        }
    }

    public async Task<EventDocument> CreateAsync(EventDocument document)
    {
        try
        {
            using var conn = db.Create();
            document.EventDocumentId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO event_document
                       (client_id, event_id, file_name, content_type, file_size, file_data,
                        extracted_text, created_by_id)
                VALUES (@clientId, @EventId, @FileName, @ContentType, @FileSize, @FileData,
                        @ExtractedText, @CreatedById)
                RETURNING event_document_id
                """, new { clientId = clientContext.RequireClientId(), document.EventId, document.FileName,
                            document.ContentType, document.FileSize, document.FileData,
                            document.ExtractedText, document.CreatedById });
            document.CreatedDate = DateTimeOffset.UtcNow;
            document.FileData = [];
            document.ExtractedText = string.Empty;
            return document;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventDocumentRepository));
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync(
                "DELETE FROM event_document WHERE event_document_id = @id AND client_id=@clientId",
                new { id, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EventDocumentRepository));
            throw;
        }
    }
}
