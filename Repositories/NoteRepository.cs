using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class NoteRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : INoteRepository
{
    private const string SelectColumns =
        """
        SELECT n.note_id,
               n.link_object_type_id,
               n.link_object_id,
               n.subject,
               n.note_text,
               n.created_date,
               n.created_by_id,
               creator.first_name AS created_by_first_name,
               creator.last_name AS created_by_last_name,
               creator.email AS created_by_email,
               n.updated_date,
               n.updated_by_id
          FROM note n
          LEFT JOIN app_user creator
            ON creator.user_id = n.created_by_id
           AND creator.client_id = n.client_id
        """;

    public async Task<IEnumerable<Note>> GetAllAsync(int? linkObjectTypeId = null, int? linkObjectId = null)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<Note>(
                $"""
                {SelectColumns}
                 WHERE n.client_id = @clientId
                   AND (@linkObjectTypeId IS NULL OR n.link_object_type_id = @linkObjectTypeId)
                   AND (@linkObjectId IS NULL OR n.link_object_id = @linkObjectId)
                 ORDER BY COALESCE(n.updated_date, n.created_date) DESC, n.note_id DESC
                """,
                new { linkObjectTypeId, linkObjectId, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(NoteRepository));
            throw;
        }
    }

    public async Task<Note?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<Note>(
                $"""
                {SelectColumns}
                 WHERE n.note_id = @id AND n.client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(NoteRepository));
            throw;
        }
    }

    public async Task<bool> LinkTargetExistsAsync(int linkObjectTypeId, int linkObjectId)
    {
        try
        {
            using var conn = db.Create();
            return linkObjectTypeId switch
            {
                1 => await conn.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS (SELECT 1 FROM contact WHERE contact_id = @linkObjectId AND client_id=@clientId)",
                    new { linkObjectId, clientId = clientContext.RequireClientId() }),
                2 => await conn.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS (SELECT 1 FROM home_event WHERE event_id = @linkObjectId AND client_id=@clientId)",
                    new { linkObjectId, clientId = clientContext.RequireClientId() }),
                _ => false
            };
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(NoteRepository));
            throw;
        }
    }

    public async Task<Note> CreateAsync(Note note)
    {
        try
        {
            using var conn = db.Create();
            note.NoteId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO note
                       (client_id, link_object_type_id, link_object_id, subject, note_text,
                        created_date, created_by_id)
                VALUES (@clientId, @LinkObjectTypeId, @LinkObjectId, @Subject, @NoteText,
                        CURRENT_TIMESTAMP, @CreatedById)
                RETURNING note_id
                """,
                new { clientId = clientContext.RequireClientId(), note.LinkObjectTypeId,
                      note.LinkObjectId, note.Subject, note.NoteText, note.CreatedById });
            return await conn.QuerySingleAsync<Note>(
                $"""
                {SelectColumns}
                 WHERE n.note_id = @noteId AND n.client_id = @clientId
                """,
                new { noteId = note.NoteId, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(NoteRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Note note)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE note
                   SET link_object_type_id = @LinkObjectTypeId,
                       link_object_id      = @LinkObjectId,
                       subject             = @Subject,
                       note_text           = @NoteText,
                       updated_date        = CURRENT_TIMESTAMP,
                       updated_by_id       = @UpdatedById
                 WHERE note_id = @NoteId AND client_id = @clientId
                """,
                new { note.NoteId, note.LinkObjectTypeId, note.LinkObjectId, note.Subject,
                      note.NoteText, note.UpdatedById, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(NoteRepository));
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync("DELETE FROM note WHERE note_id = @id AND client_id = @clientId",
                new { id, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(NoteRepository));
            throw;
        }
    }
}
