using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class ContactRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : IContactRepository
{
    public async Task<IEnumerable<Contact>> GetAllAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<Contact>(
                """
                SELECT contact_id,
                       first_name,
                       last_name,
                       email,
                       mobile,
                       company_name
                  FROM contact
                 WHERE client_id = @clientId
                 ORDER BY last_name, first_name
                """, new { clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ContactRepository));
            throw;
        }
    }

    public async Task<Contact?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<Contact>(
                """
                SELECT contact_id,
                       first_name,
                       last_name,
                       email,
                       mobile,
                       company_name
                  FROM contact
                 WHERE contact_id = @id AND client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ContactRepository));
            throw;
        }
    }

    public async Task<Contact> CreateAsync(Contact contact)
    {
        try
        {
            using var conn = db.Create();
            contact.ContactId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO contact (client_id, first_name, last_name, email, mobile, company_name)
                VALUES (@clientId, @FirstName, @LastName, @Email, @Mobile, @CompanyName)
                RETURNING contact_id
                """,
                new { clientId = clientContext.RequireClientId(), contact.FirstName, contact.LastName,
                      contact.Email, contact.Mobile, contact.CompanyName });
            return contact;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ContactRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Contact contact)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE contact
                   SET first_name   = @FirstName,
                       last_name    = @LastName,
                       email        = @Email,
                       mobile       = @Mobile,
                       company_name = @CompanyName
                 WHERE contact_id = @ContactId AND client_id = @clientId
                """,
                new { contact.ContactId, contact.FirstName, contact.LastName, contact.Email,
                      contact.Mobile, contact.CompanyName, clientId = clientContext.RequireClientId() });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ContactRepository));
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            await conn.ExecuteAsync(
                "DELETE FROM event_contact_link WHERE contact_id = @id AND client_id = @clientId",
                new { id, clientId = clientContext.RequireClientId() }, transaction);
            var rows = await conn.ExecuteAsync(
                "DELETE FROM contact WHERE contact_id = @id AND client_id = @clientId",
                new { id, clientId = clientContext.RequireClientId() }, transaction);
            transaction.Commit();
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ContactRepository));
            throw;
        }
    }
}
