using System.Text;
using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public class HomeEventsRepository(DbConnectionFactory db, ErrorLogRepository errorLog) : IHomeEventsRepository
{
    // All read queries use this SELECT + LEFT JOINs to pull lookup titles in one round-trip.
    // Columns are aliased so Dapper's MatchNamesWithUnderscores maps them to HomeEventDetail properties.
    private const string SelectWithJoins =
        """
        SELECT he.event_id,
               he.title,
               he.description,
               he.event_date,
               he.created_date,
               he.created_by_id,
               he.updated_date,
               he.event_type_id,
               he.area_id,
               he.event_status_id,
               et.title     AS event_type_title,
               a.title      AS area_title,
               es.title     AS event_status_title,
               u.first_name AS created_by_first_name,
               u.last_name  AS created_by_last_name
          FROM home_event he
          LEFT JOIN event_type   et ON et.event_type_id   = he.event_type_id
          LEFT JOIN area          a ON a.area_id           = he.area_id
          LEFT JOIN event_status es ON es.event_status_id  = he.event_status_id
          LEFT JOIN app_user      u ON u.user_id           = he.created_by_id
        """;

    public async Task<IEnumerable<HomeEventDetail>> GetByFilterAsync(HomeEventFilter filter)
    {
        try
        {
            var sql = new StringBuilder(SelectWithJoins).AppendLine(" WHERE 1=1");
            var p   = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.TitleContains))
            {
                sql.AppendLine(" AND LOWER(he.title) LIKE LOWER(@TitleContains)");
                p.Add("TitleContains", $"%{filter.TitleContains}%");
            }
            if (!string.IsNullOrWhiteSpace(filter.DescriptionContains))
            {
                sql.AppendLine(" AND LOWER(he.description) LIKE LOWER(@DescriptionContains)");
                p.Add("DescriptionContains", $"%{filter.DescriptionContains}%");
            }
            if (filter.EventTypeId.HasValue)
            {
                sql.AppendLine(" AND he.event_type_id = @EventTypeId");
                p.Add("EventTypeId", filter.EventTypeId.Value);
            }
            if (filter.AreaId.HasValue)
            {
                sql.AppendLine(" AND he.area_id = @AreaId");
                p.Add("AreaId", filter.AreaId.Value);
            }
            if (filter.EventStatusIds is { Count: > 0 })
            {
                sql.AppendLine(" AND he.event_status_id = ANY(@EventStatusIds)");
                p.Add("EventStatusIds", filter.EventStatusIds.ToArray());
            }
            else if (filter.EventStatusId.HasValue)
            {
                sql.AppendLine(" AND he.event_status_id = @EventStatusId");
                p.Add("EventStatusId", filter.EventStatusId.Value);
            }
            if (filter.CreatedById.HasValue)
            {
                sql.AppendLine(" AND he.created_by_id = @CreatedById");
                p.Add("CreatedById", filter.CreatedById.Value);
            }
            if (filter.EventDateFrom.HasValue)
            {
                sql.AppendLine(" AND he.event_date >= @EventDateFrom");
                p.Add("EventDateFrom", filter.EventDateFrom.Value);
            }
            if (filter.EventDateTo.HasValue)
            {
                sql.AppendLine(" AND he.event_date <= @EventDateTo");
                p.Add("EventDateTo", filter.EventDateTo.Value);
            }
            if (filter.CreatedDateFrom.HasValue)
            {
                sql.AppendLine(" AND he.created_date >= @CreatedDateFrom");
                p.Add("CreatedDateFrom", filter.CreatedDateFrom.Value);
            }
            if (filter.CreatedDateTo.HasValue)
            {
                sql.AppendLine(" AND he.created_date <= @CreatedDateTo");
                p.Add("CreatedDateTo", filter.CreatedDateTo.Value);
            }
            if (filter.UpdatedDateFrom.HasValue)
            {
                sql.AppendLine(" AND he.updated_date >= @UpdatedDateFrom");
                p.Add("UpdatedDateFrom", filter.UpdatedDateFrom.Value);
            }
            if (filter.UpdatedDateTo.HasValue)
            {
                sql.AppendLine(" AND he.updated_date <= @UpdatedDateTo");
                p.Add("UpdatedDateTo", filter.UpdatedDateTo.Value);
            }

            sql.Append(" ORDER BY he.event_date DESC");

            using var conn = db.Create();
            return await conn.QueryAsync<HomeEventDetail>(sql.ToString(), p);
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }

    public async Task<HomeEventDetail?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<HomeEventDetail>(
                $"""
                {SelectWithJoins}
                 WHERE he.event_id = @id
                """,
                new { id });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }

    public async Task<IEnumerable<HomeEventDetail>> GetByUserAsync(int userId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<HomeEventDetail>(
                $"""
                {SelectWithJoins}
                 WHERE he.created_by_id = @userId
                 ORDER BY he.event_date DESC
                """,
                new { userId });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }

    public async Task<HomeEvent> CreateAsync(HomeEvent homeEvent)
    {
        try
        {
            using var conn = db.Create();
            homeEvent.EventId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO home_event
                       (title, description, event_date, created_date, created_by_id,
                        updated_date, event_type_id, area_id, event_status_id)
                VALUES (@Title, @Description, @EventDate, now(), @CreatedById,
                        @UpdatedDate, @EventTypeId, @AreaId, @EventStatusId)
                RETURNING event_id
                """,
                homeEvent);
            return homeEvent;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(HomeEvent homeEvent)
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.ExecuteAsync(
                """
                UPDATE home_event
                   SET title           = @Title,
                       description     = @Description,
                       event_date      = @EventDate,
                       updated_date    = now(),
                       event_type_id   = @EventTypeId,
                       area_id         = @AreaId,
                       event_status_id = @EventStatusId
                 WHERE event_id = @EventId
                """,
                homeEvent);
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
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
                DELETE FROM home_event
                 WHERE event_id = @id
                """,
                new { id });
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }
}
