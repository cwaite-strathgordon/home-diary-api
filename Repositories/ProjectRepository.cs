using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class ProjectRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext) : IProjectRepository
{
    private const string SelectProjects =
        """
        SELECT p.project_id, p.title, p.description, p.area_id, a.title AS area_title, p.start_date,
               p.target_completion_date, p.created_date, p.created_by_id, p.updated_date,
               p.status, p.archived_date,
               COUNT(he.event_id)::int AS total_tasks,
               COUNT(he.event_id) FILTER (WHERE he.actual_completion_date IS NULL)::int AS active_tasks,
               COUNT(he.event_id) FILTER (WHERE he.actual_completion_date IS NOT NULL)::int AS completed_tasks,
               COUNT(he.event_id) FILTER (
                   WHERE he.actual_completion_date IS NULL
                     AND he.target_completion_date < CURRENT_DATE)::int AS overdue_tasks
          FROM project p
          LEFT JOIN area a ON a.area_id = p.area_id AND a.client_id = p.client_id
          LEFT JOIN home_event he ON he.project_id = p.project_id AND he.client_id = p.client_id
        """;

    public async Task<IEnumerable<Project>> GetAllAsync(bool includeArchived = false)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QueryAsync<Project>(
                $"{SelectProjects} WHERE p.client_id=@clientId AND (@includeArchived OR p.status <> 'Archived') GROUP BY p.project_id, a.title ORDER BY p.title",
                new { includeArchived, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ProjectRepository));
            throw;
        }
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<Project>(
                $"{SelectProjects} WHERE p.project_id = @id AND p.client_id=@clientId GROUP BY p.project_id, a.title",
                new { id, clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ProjectRepository));
            throw;
        }
    }

    public async Task<Project> CreateAsync(Project project)
    {
        try
        {
            using var conn = db.Create();
            project.ProjectId = await conn.QuerySingleAsync<int>(
                """
                INSERT INTO project
                       (client_id, title, description, area_id, start_date, target_completion_date, status,
                        created_date, created_by_id)
                VALUES (@clientId, @Title, @Description, @AreaId, @StartDate, @TargetCompletionDate, @Status,
                        CURRENT_DATE, @CreatedById)
                RETURNING project_id
                """, new { clientId = clientContext.RequireClientId(), project.Title, project.Description, project.AreaId,
                            project.StartDate, project.TargetCompletionDate, project.Status, project.CreatedById });
            project.CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            return project;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ProjectRepository));
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Project project)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync(
                """
                UPDATE project
                   SET title = @Title,
                       description = @Description,
                       area_id = @AreaId,
                       start_date = @StartDate,
                       target_completion_date = @TargetCompletionDate,
                       status = @Status,
                       updated_date = CURRENT_DATE
                 WHERE project_id = @ProjectId AND client_id = @clientId
                """, new { project.ProjectId, project.Title, project.Description, project.AreaId, project.StartDate,
                            project.TargetCompletionDate, project.Status, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ProjectRepository));
            throw;
        }
    }

    public async Task<bool> ArchiveAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync(
                """
                UPDATE project p
                   SET status = 'Archived',
                       archived_date = CURRENT_DATE,
                       updated_date = CURRENT_DATE
                 WHERE p.project_id = @id
                   AND p.client_id = @clientId
                   AND NOT EXISTS
                       (SELECT 1
                          FROM home_event he
                         WHERE he.project_id = p.project_id
                           AND he.actual_completion_date IS NULL)
                """, new { id, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ProjectRepository));
            throw;
        }
    }

    public async Task<bool> RestoreAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync(
                """
                UPDATE project
                   SET status = 'Active',
                       archived_date = NULL,
                       updated_date = CURRENT_DATE
                 WHERE project_id = @id
                   AND client_id = @clientId
                   AND status = 'Archived'
                """, new { id, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ProjectRepository));
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync("DELETE FROM project WHERE project_id = @id AND client_id = @clientId",
                new { id, clientId = clientContext.RequireClientId() }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ProjectRepository));
            throw;
        }
    }
}
