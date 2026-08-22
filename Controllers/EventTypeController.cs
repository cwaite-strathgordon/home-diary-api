using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/event-types")]
public class EventTypeController(IEventTypeRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventType>>> GetAll()
    {
        var types = await repo.GetAllAsync();
        return Ok(types);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventType>> GetById(int id)
    {
        var type = await repo.GetByIdAsync(id);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost]
    public async Task<ActionResult<EventType>> Create(EventType type)
    {
        type.Title = type.Title?.Trim();
        type.Description = type.Description?.Trim();

        if (string.IsNullOrWhiteSpace(type.Title))
            return BadRequest("Title is required.");

        if (await repo.TitleExistsAsync(type.Title!))
            return Conflict($"An event type named '{type.Title}' already exists.");

        type.EventTypeId = 0;
        var created = await repo.CreateAsync(type);
        return CreatedAtAction(nameof(GetById), new { id = created.EventTypeId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventType type)
    {
        if (id != type.EventTypeId) return BadRequest("Route id does not match body EventTypeId.");

        type.Title = type.Title?.Trim();
        type.Description = type.Description?.Trim();

        if (string.IsNullOrWhiteSpace(type.Title))
            return BadRequest("Title is required.");

        if (await repo.TitleExistsAsync(type.Title!, id))
            return Conflict($"An event type named '{type.Title}' already exists.");

        var updated = await repo.UpdateAsync(type);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await repo.GetByIdAsync(id) is null) return NotFound();
        if (await repo.IsInUseAsync(id))
            return Conflict("The event type cannot be deleted because it is used by one or more home events.");

        var deleted = await repo.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
