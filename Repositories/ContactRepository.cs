using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class ContactRepository(DbConnectionFactory db, ErrorLogRepository errorLog) : IContactRepository
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
                 ORDER BY last_name, first_name
                """);
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
                 WHERE contact_id = @id
                """,
                new { id });
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
                INSERT INTO contact (first_name, last_name, email, mobile, company_name)
                VALUES (@FirstName, @LastName, @Email, @Mobile, @CompanyName)
                RETURNING contact_id
                """,
                contact);
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
                 WHERE contact_id = @ContactId
                """,
                contact);
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
            var rows = await conn.ExecuteAsync(
                """
                DELETE FROM contact
                 WHERE contact_id = @id
                """,
                new { id });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ContactRepository));
            throw;
        }
    }
}
