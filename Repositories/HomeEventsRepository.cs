using System.Text;
using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class HomeEventsRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : IHomeEventsRepository
{
    // All read queries use this SELECT + LEFT JOINs to pull lookup titles in one round-trip.
    // Columns are aliased so Dapper's MatchNamesWithUnderscores maps them to HomeEventDetail properties.
    private const string SelectWithJoins =
        """
        SELECT he.event_id,
               he.title,
               he.description,
               he.event_date,
               he.target_completion_date,
               he.actual_completion_date,
               he.is_recurring,
               he.recurrence_interval,
               he.recurrence_unit,
               he.created_date,
               he.created_by_id,
               he.updated_date,
               he.event_type_id,
               he.area_id,
               he.event_status_id,
               he.priority_id,
               he.project_id,
               et.title     AS event_type_title,
               a.title      AS area_title,
               es.title     AS event_status_title,
               ep.title     AS priority_title,
               p.title      AS project_title,
               u.first_name AS created_by_first_name,
               u.last_name  AS created_by_last_name
          FROM home_event he
          LEFT JOIN event_type   et ON et.event_type_id   = he.event_type_id
          LEFT JOIN area          a ON a.area_id           = he.area_id
          LEFT JOIN event_status es ON es.event_status_id  = he.event_status_id
          LEFT JOIN event_priority ep ON ep.event_priority_id = he.priority_id
          LEFT JOIN project        p ON p.project_id           = he.project_id
          LEFT JOIN app_user      u ON u.user_id           = he.created_by_id
        """;

    public async Task<IEnumerable<HomeEventDetail>> GetByFilterAsync(HomeEventFilter filter)
    {
        try
        {
            var sql = new StringBuilder(SelectWithJoins).AppendLine(" WHERE 1=1");
            var p   = new DynamicParameters();
            sql.AppendLine(" AND he.client_id = @ClientId");
            p.Add("ClientId", clientContext.RequireClientId());

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
            if (filter.PriorityId.HasValue)
            {
                sql.AppendLine(" AND he.priority_id = @PriorityId");
                p.Add("PriorityId", filter.PriorityId.Value);
            }
            if (filter.ProjectId.HasValue)
            {
                if (filter.ProjectId.Value == 0)
                    sql.AppendLine(" AND he.project_id IS NULL");
                else
                {
                    sql.AppendLine(" AND he.project_id = @ProjectId");
                    p.Add("ProjectId", filter.ProjectId.Value);
                }
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
            if (filter.TargetCompletionDateFrom.HasValue)
            {
                sql.AppendLine(" AND he.target_completion_date >= @TargetCompletionDateFrom");
                p.Add("TargetCompletionDateFrom", filter.TargetCompletionDateFrom.Value);
            }
            if (filter.TargetCompletionDateTo.HasValue)
            {
                sql.AppendLine(" AND he.target_completion_date <= @TargetCompletionDateTo");
                p.Add("TargetCompletionDateTo", filter.TargetCompletionDateTo.Value);
            }
            if (filter.ActualCompletionDateFrom.HasValue)
            {
                sql.AppendLine(" AND he.actual_completion_date >= @ActualCompletionDateFrom");
                p.Add("ActualCompletionDateFrom", filter.ActualCompletionDateFrom.Value);
            }
            if (filter.ActualCompletionDateTo.HasValue)
            {
                sql.AppendLine(" AND he.actual_completion_date <= @ActualCompletionDateTo");
                p.Add("ActualCompletionDateTo", filter.ActualCompletionDateTo.Value);
            }
            if (filter.Overdue == true)
                sql.AppendLine(" AND he.target_completion_date < CURRENT_DATE AND he.actual_completion_date IS NULL");
            if (filter.ActiveOnly == true)
                sql.AppendLine(" AND he.actual_completion_date IS NULL");
            if (filter.ExcludeWishList == true)
                sql.AppendLine(
                    " AND LOWER(COALESCE(ep.title, '')) <> 'wish list'" +
                    " AND LOWER(COALESCE(es.title, '')) <> 'wish list'");
            if (filter.RecurringOnly == true)
                sql.AppendLine(
                    " AND he.is_recurring = true" +
                    " AND he.recurrence_interval > 0" +
                    " AND NULLIF(TRIM(he.recurrence_unit), '') IS NOT NULL" +
                    " AND he.event_date IS NOT NULL");
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

    public async Task<EventTaskSummary> GetTaskSummaryAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleAsync<EventTaskSummary>(
                """
                SELECT COUNT(*) FILTER (
                           WHERE he.actual_completion_date IS NULL
                             AND LOWER(COALESCE(ep.title, '')) <> 'wish list'
                             AND LOWER(COALESCE(es.title, '')) <> 'wish list'
                       ) AS all_active_tasks,
                       COUNT(*) FILTER (
                           WHERE he.actual_completion_date IS NULL
                             AND he.target_completion_date < CURRENT_DATE
                       ) AS overdue_tasks,
                       COUNT(*) FILTER (
                           WHERE he.actual_completion_date IS NULL
                             AND he.target_completion_date BETWEEN CURRENT_DATE AND CURRENT_DATE + 7
                       ) AS due_next_seven_days,
                       COUNT(*) FILTER (
                           WHERE he.actual_completion_date IS NULL
                             AND LOWER(ep.title) = 'critical'
                       ) AS critical_tasks,
                       COUNT(*) FILTER (
                           WHERE he.actual_completion_date >= CURRENT_DATE - INTERVAL '1 month'
                             AND he.actual_completion_date <= CURRENT_DATE
                       ) AS completed_last_month,
                       COUNT(*) FILTER (
                           WHERE he.created_date >= CURRENT_DATE - 7
                             AND he.created_date <= CURRENT_DATE
                       ) AS created_last_seven_days
                  FROM home_event he
                  LEFT JOIN event_priority ep ON ep.event_priority_id = he.priority_id
                  LEFT JOIN event_status es ON es.event_status_id = he.event_status_id
                 WHERE he.client_id = @clientId
                """, new { clientId = clientContext.RequireClientId() });
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
                 WHERE he.event_id = @id AND he.client_id = @clientId
                """,
                new { id, clientId = clientContext.RequireClientId() });
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
                 WHERE he.created_by_id = @userId AND he.client_id = @clientId
                 ORDER BY he.event_date DESC
                """,
                new { userId, clientId = clientContext.RequireClientId() });
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
                       (client_id, title, description, event_date, created_date, created_by_id,
                        updated_date, event_type_id, area_id, event_status_id, priority_id,
                        target_completion_date, actual_completion_date, is_recurring,
                        recurrence_interval, recurrence_unit, project_id)
                VALUES (@ClientId, @Title, @Description, @EventDate, now(), @CreatedById,
                        @UpdatedDate, @EventTypeId, @AreaId, @EventStatusId, @PriorityId,
                        @TargetCompletionDate, @ActualCompletionDate, @IsRecurring,
                        @RecurrenceInterval, @RecurrenceUnit, @ProjectId)
                RETURNING event_id
                """,
                WithClient(homeEvent));
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
                       event_status_id = @EventStatusId,
                       priority_id     = @PriorityId
                      ,target_completion_date = @TargetCompletionDate
                      ,actual_completion_date = @ActualCompletionDate
                      ,is_recurring           = @IsRecurring
                      ,recurrence_interval    = @RecurrenceInterval
                      ,recurrence_unit        = @RecurrenceUnit
                      ,project_id              = @ProjectId
                 WHERE event_id = @EventId AND client_id = @ClientId
                """,
                WithClient(homeEvent));
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
            conn.Open();
            using var transaction = conn.BeginTransaction();
            await conn.ExecuteAsync(
                "DELETE FROM event_contact_link WHERE event_id = @id AND client_id=@clientId",
                new { id, clientId = clientContext.RequireClientId() }, transaction);
            await conn.ExecuteAsync(
                "DELETE FROM note WHERE link_object_type_id = 2 AND link_object_id = @id AND client_id=@clientId",
                new { id, clientId = clientContext.RequireClientId() }, transaction);
            var rows = await conn.ExecuteAsync(
                """
                DELETE FROM home_event
                 WHERE event_id = @id AND client_id=@clientId
                """,
                new { id, clientId = clientContext.RequireClientId() }, transaction);
            transaction.Commit();
            return rows > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }

    public async Task<CompleteEventResult?> CompleteAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var homeEvent = await conn.QuerySingleOrDefaultAsync<HomeEvent>(
                """
                SELECT event_id, title, description, event_date, target_completion_date,
                       actual_completion_date, created_by_id, event_type_id, area_id,
                       event_status_id, priority_id, is_recurring, recurrence_interval,
                       recurrence_unit, project_id
                  FROM home_event
                 WHERE event_id = @id AND client_id=@clientId
                   FOR UPDATE
                """, new { id, clientId = clientContext.RequireClientId() }, transaction);
            if (homeEvent is null) return null;

            if (homeEvent.ActualCompletionDate.HasValue)
            {
                transaction.Commit();
                return new CompleteEventResult { CompletedEventId = id };
            }

            var completeStatusId = await conn.QuerySingleAsync<int>(
                "SELECT event_status_id FROM event_status WHERE LOWER(title) = 'complete' LIMIT 1",
                transaction: transaction);
            await conn.ExecuteAsync(
                """
                UPDATE home_event
                   SET event_status_id = @completeStatusId,
                       actual_completion_date = CURRENT_DATE,
                       updated_date = CURRENT_DATE
                 WHERE event_id = @id
                   AND client_id = @clientId
                """, new { id, completeStatusId, clientId = clientContext.RequireClientId() }, transaction);

            int? nextEventId = null;
            if (homeEvent.IsRecurring && homeEvent.EventDate.HasValue
                && homeEvent.RecurrenceInterval is > 0 && !string.IsNullOrWhiteSpace(homeEvent.RecurrenceUnit))
            {
                var nextStart = AddInterval(homeEvent.EventDate.Value, homeEvent.RecurrenceInterval.Value, homeEvent.RecurrenceUnit);
                DateOnly? nextTarget = null;
                if (homeEvent.TargetCompletionDate.HasValue)
                    nextTarget = nextStart.AddDays(homeEvent.TargetCompletionDate.Value.DayNumber - homeEvent.EventDate.Value.DayNumber);
                var pendingStatusId = await conn.QuerySingleAsync<int>(
                    "SELECT event_status_id FROM event_status WHERE LOWER(title) = 'pending' LIMIT 1",
                    transaction: transaction);
                nextEventId = await conn.QuerySingleAsync<int>(
                    """
                    INSERT INTO home_event
                           (client_id, title, description, event_date, target_completion_date, created_date,
                            created_by_id, event_type_id, area_id, event_status_id, priority_id,
                            is_recurring, recurrence_interval, recurrence_unit, project_id)
                    VALUES (@clientId, @Title, @Description, @nextStart, @nextTarget, CURRENT_DATE,
                            @CreatedById, @EventTypeId, @AreaId, @pendingStatusId, @PriorityId,
                            true, @RecurrenceInterval, @RecurrenceUnit, @ProjectId)
                    RETURNING event_id
                    """, new
                    {
                        homeEvent.Title, homeEvent.Description, nextStart, nextTarget,
                        homeEvent.CreatedById, homeEvent.EventTypeId, homeEvent.AreaId,
                        pendingStatusId, homeEvent.PriorityId, homeEvent.RecurrenceInterval,
                        homeEvent.RecurrenceUnit, homeEvent.ProjectId,
                        clientId = clientContext.RequireClientId()
                    }, transaction);
                await conn.ExecuteAsync(
                    """
                    INSERT INTO event_contact_link (client_id, contact_id, event_id)
                    SELECT @clientId, contact_id, @nextEventId
                      FROM event_contact_link
                     WHERE event_id = @id
                       AND client_id = @clientId
                    """, new { id, nextEventId, clientId = clientContext.RequireClientId() }, transaction);
            }

            transaction.Commit();
            return new CompleteEventResult { CompletedEventId = id, NextEventId = nextEventId };
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }

    public async Task<bool> ReopenAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync(
                """
                UPDATE home_event
                   SET event_status_id = (SELECT event_status_id FROM event_status WHERE LOWER(title) = 'pending' LIMIT 1),
                       actual_completion_date = NULL,
                       updated_date = CURRENT_DATE
                 WHERE event_id = @id AND client_id=@clientId
                """, new { id, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(HomeEventsRepository));
            throw;
        }
    }

    private static DateOnly AddInterval(DateOnly date, int interval, string unit) => unit.ToLowerInvariant() switch
    {
        "day" => date.AddDays(interval),
        "week" => date.AddDays(interval * 7),
        "month" => date.AddMonths(interval),
        "year" => date.AddYears(interval),
        _ => throw new InvalidOperationException("Unsupported recurrence unit.")
    };

    private object WithClient(HomeEvent e) => new
    {
        ClientId = clientContext.RequireClientId(), e.Title, e.Description, e.EventDate,
        e.CreatedById, e.UpdatedDate, e.EventTypeId, e.AreaId, e.EventStatusId,
        e.PriorityId, e.TargetCompletionDate, e.ActualCompletionDate, e.IsRecurring,
        e.RecurrenceInterval, e.RecurrenceUnit, e.ProjectId, e.EventId
    };
}
