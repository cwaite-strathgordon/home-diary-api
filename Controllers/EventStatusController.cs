using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/event-statuses")]
public class EventStatusController(IEventStatusRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventStatus>>> GetAll()
    {
        var statuses = await repo.GetAllAsync();
        return Ok(statuses);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventStatus>> GetById(int id)
    {
        var status = await repo.GetByIdAsync(id);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpPost]
    public async Task<ActionResult<EventStatus>> Create(EventStatus status)
    {
        var created = await repo.CreateAsync(status);
        return CreatedAtAction(nameof(GetById), new { id = created.EventStatusId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventStatus status)
    {
        if (id != status.EventStatusId) return BadRequest("Route id does not match body EventStatusId.");
        var updated = await repo.UpdateAsync(status);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await repo.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
