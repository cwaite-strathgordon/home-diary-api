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

    [HttpGet("task-summary")]
    public async Task<ActionResult<EventTaskSummary>> GetTaskSummary()
    {
        return Ok(await repo.GetTaskSummaryAsync());
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
        homeEvent.CreatedById = CurrentUserId();
        var recurrenceError = ValidateRecurrence(homeEvent);
        if (recurrenceError is not null) return BadRequest(recurrenceError);

        homeEvent.CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var created = await repo.CreateAsync(homeEvent);
        return CreatedAtAction(nameof(GetById), new { id = created.EventId }, created);
    }

    private int CurrentUserId() => int.TryParse(
        User.FindFirst("homediary_user_id")?.Value, out var id)
        ? id : throw new InvalidOperationException("Authenticated user ID is missing.");

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, HomeEvent homeEvent)
    {
        if (id != homeEvent.EventId) return BadRequest("Route id does not match body EventId.");
        var recurrenceError = ValidateRecurrence(homeEvent);
        if (recurrenceError is not null) return BadRequest(recurrenceError);
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

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<CompleteEventResult>> Complete(int id)
    {
        var result = await repo.CompleteAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> Reopen(int id)
    {
        return await repo.ReopenAsync(id) ? NoContent() : NotFound();
    }

    private static string? ValidateRecurrence(HomeEvent homeEvent)
    {
        if (!homeEvent.IsRecurring) return null;
        if (!homeEvent.EventDate.HasValue) return "A recurring task requires a start date.";
        if (homeEvent.RecurrenceInterval is null or <= 0) return "A recurring task requires a positive recurrence interval.";
        if (homeEvent.RecurrenceUnit is not ("day" or "week" or "month" or "year"))
            return "Recurrence unit must be day, week, month, or year.";
        return null;
    }
}
