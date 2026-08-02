using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/home-events")]
public class HomeEventsController(IHomeEventsRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HomeEventDetail>>> GetByFilter([FromQuery] HomeEventFilter filter)
    {
        var events = await repo.GetByFilterAsync(filter);
        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HomeEventDetail>> GetById(int id)
    {
        var ev = await repo.GetByIdAsync(id);
        return ev is null ? NotFound() : Ok(ev);
    }

    [HttpGet("by-user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<HomeEventDetail>>> GetByUser(int userId)
    {
        var events = await repo.GetByUserAsync(userId);
        return Ok(events);
    }

    [HttpPost]
    public async Task<ActionResult<HomeEvent>> Create(HomeEvent homeEvent)
    {
        if (homeEvent.CreatedById is null or 0)
            return BadRequest("CreatedById (UserId) is required.");

        homeEvent.CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var created = await repo.CreateAsync(homeEvent);
        return CreatedAtAction(nameof(GetById), new { id = created.EventId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, HomeEvent homeEvent)
    {
        if (id != homeEvent.EventId) return BadRequest("Route id does not match body EventId.");
        homeEvent.UpdatedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var updated = await repo.UpdateAsync(homeEvent);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await repo.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
