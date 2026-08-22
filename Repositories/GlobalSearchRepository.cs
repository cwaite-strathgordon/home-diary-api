using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class GlobalSearchRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext)
    : IGlobalSearchRepository
{
    public async Task<IEnumerable<GlobalSearchResult>> SearchAsync(string query, int limit = 30)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<GlobalSearchResult>(
                """
                WITH search AS
                (
                    SELECT websearch_to_tsquery('english', @query) AS terms,
                           '%' || LOWER(@query) || '%' AS contains
                ), results AS
                (
                    SELECT 'event'::text AS result_type, he.event_id AS object_id,
                           NULL::integer AS parent_id, he.title::text AS title,
                           COALESCE(et.title || ' · ', '') || COALESCE(a.title, 'Task') AS subtitle,
                           ts_headline('english', COALESCE(he.description, ''), search.terms,
                               'MaxWords=18, MinWords=6, StartSel=<mark>, StopSel=</mark>') AS search_snippet,
                           ts_rank(he.search_vector, search.terms) + 0.25::real AS rank
                      FROM home_event he
                      LEFT JOIN event_type et ON et.event_type_id = he.event_type_id
                      LEFT JOIN area a ON a.area_id = he.area_id, search
                     WHERE he.client_id = @clientId
                       AND (he.search_vector @@ search.terms OR LOWER(he.title) LIKE search.contains)

                    UNION ALL

                    SELECT 'contact', c.contact_id, NULL,
                           COALESCE(NULLIF(TRIM(COALESCE(c.first_name, '') || ' ' || COALESCE(c.last_name, '')), ''), c.company_name, 'Unnamed contact'),
                           COALESCE(c.company_name || ' · ', '') || COALESCE(c.email, c.mobile, 'Contact'),
                           '', ts_rank(c.search_vector, search.terms) + 0.20::real
                      FROM contact c, search
                     WHERE c.client_id = @clientId
                       AND (c.search_vector @@ search.terms
                        OR LOWER(COALESCE(c.first_name, '') || ' ' || COALESCE(c.last_name, '') || ' ' || COALESCE(c.company_name, '') || ' ' || COALESCE(c.email, '')) LIKE search.contains)

                    UNION ALL

                    SELECT 'document', d.event_document_id, d.event_id, d.file_name,
                           'Document · ' || COALESCE(he.title, 'Event'),
                           ts_headline('english', d.extracted_text, search.terms,
                               'MaxWords=22, MinWords=8, StartSel=<mark>, StopSel=</mark>'),
                           ts_rank(d.search_vector, search.terms) + 0.15::real
                      FROM event_document d
                      JOIN home_event he ON he.event_id = d.event_id, search
                     WHERE d.client_id = @clientId
                       AND (d.search_vector @@ search.terms OR LOWER(d.file_name) LIKE search.contains)

                    UNION ALL

                    SELECT CASE WHEN n.link_object_type_id = 1 THEN 'contact-note' ELSE 'event-note' END,
                           n.note_id, n.link_object_id, COALESCE(NULLIF(n.subject, ''), 'Note'),
                           CASE WHEN n.link_object_type_id = 1 THEN 'Contact note' ELSE 'Task note' END,
                           ts_headline('english', n.note_text, search.terms,
                               'MaxWords=20, MinWords=7, StartSel=<mark>, StopSel=</mark>'),
                           ts_rank(n.search_vector, search.terms) + 0.05::real
                      FROM note n, search
                     WHERE n.client_id = @clientId AND n.search_vector @@ search.terms
                )
                SELECT result_type, object_id, parent_id, title, subtitle, search_snippet, rank
                  FROM results
                 ORDER BY rank DESC, title
                 LIMIT @limit
                """, new { query, limit, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(GlobalSearchRepository));
            throw;
        }
    }
}
