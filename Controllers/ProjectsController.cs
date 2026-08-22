using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(IProjectRepository repo, IAreaRepository areas) : ControllerBase
{
    private static readonly HashSet<string> EditableStatuses =
        ["Wish List", "Active", "On Hold"];

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetAll([FromQuery] bool includeArchived = false) =>
        Ok(await repo.GetAllAsync(includeArchived));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Project>> GetById(int id)
    {
        var project = await repo.GetByIdAsync(id);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<Project>> Create(Project project)
    {
        project.Title = project.Title?.Trim();
        project.Description = project.Description?.Trim();
        project.Status = project.Status?.Trim() ?? "Active";
        if (string.IsNullOrWhiteSpace(project.Title)) return BadRequest("Title is required.");
        if (project.AreaId.HasValue && await areas.GetByIdAsync(project.AreaId.Value) is null)
            return BadRequest("The selected area is not available for this client.");
        project.CreatedById = CurrentUserId();
        if (!EditableStatuses.Contains(project.Status)) return BadRequest("Invalid project status.");
        project.ProjectId = 0;
        var created = await repo.CreateAsync(project);
        return CreatedAtAction(nameof(GetById), new { id = created.ProjectId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Project project)
    {
        if (id != project.ProjectId) return BadRequest("Route id does not match body ProjectId.");
        project.Title = project.Title?.Trim();
        project.Description = project.Description?.Trim();
        project.Status = project.Status?.Trim() ?? "Active";
        if (string.IsNullOrWhiteSpace(project.Title)) return BadRequest("Title is required.");
        if (project.AreaId.HasValue && await areas.GetByIdAsync(project.AreaId.Value) is null)
            return BadRequest("The selected area is not available for this client.");
        var existing = await repo.GetByIdAsync(id);
        if (existing is null) return NotFound();
        if (project.Status == "Archived" && existing.Status != "Archived")
            return BadRequest("Use the archive action to archive a project.");
        if (existing.Status == "Archived" && project.Status != "Archived")
            return BadRequest("Use the restore action to restore an archived project.");
        if (project.Status != "Archived" && !EditableStatuses.Contains(project.Status))
            return BadRequest("Invalid project status.");
        return await repo.UpdateAsync(project) ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        if (await repo.ArchiveAsync(id)) return NoContent();
        var project = await repo.GetByIdAsync(id);
        return project is null
            ? NotFound()
            : Conflict(new ProblemDetails
            {
                Title = "Project cannot be archived",
                Detail = $"Complete the {project.ActiveTasks} remaining task(s) before archiving this project."
            });
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        if (await repo.RestoreAsync(id)) return NoContent();
        return await repo.GetByIdAsync(id) is null ? NotFound() : NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await repo.DeleteAsync(id) ? NoContent() : NotFound();
    }

    private int CurrentUserId() => int.TryParse(
        User.FindFirst("homediary_user_id")?.Value, out var id)
        ? id : throw new InvalidOperationException("Authenticated user ID is missing.");
}
