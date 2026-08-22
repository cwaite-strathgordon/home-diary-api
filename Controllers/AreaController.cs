using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/areas")]
public class AreaController(IAreaRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Area>>> GetAll()
    {
        var areas = await repo.GetAllAsync();
        return Ok(areas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Area>> GetById(int id)
    {
        var area = await repo.GetByIdAsync(id);
        return area is null ? NotFound() : Ok(area);
    }

    [HttpPost]
    public async Task<ActionResult<Area>> Create(Area area)
    {
        area.Title = area.Title?.Trim();
        area.Description = area.Description?.Trim();

        if (string.IsNullOrWhiteSpace(area.Title))
            return BadRequest("Title is required.");

        if (await repo.TitleExistsAsync(area.Title!))
            return Conflict($"An area named '{area.Title}' already exists.");

        area.AreaId = 0;
        var created = await repo.CreateAsync(area);
        return CreatedAtAction(nameof(GetById), new { id = created.AreaId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Area area)
    {
        if (id != area.AreaId) return BadRequest("Route id does not match body AreaId.");

        area.Title = area.Title?.Trim();
        area.Description = area.Description?.Trim();

        if (string.IsNullOrWhiteSpace(area.Title))
            return BadRequest("Title is required.");

        if (await repo.TitleExistsAsync(area.Title!, id))
            return Conflict($"An area named '{area.Title}' already exists.");

        var updated = await repo.UpdateAsync(area);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await repo.GetByIdAsync(id) is null) return NotFound();
        if (await repo.IsInUseAsync(id))
            return Conflict("The area cannot be deleted because it is used by one or more home events.");

        var deleted = await repo.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
