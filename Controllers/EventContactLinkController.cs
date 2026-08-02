using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/event-contact-links")]
public class EventContactLinkController(IEventContactLinkRepository repo) : ControllerBase
{
    [HttpGet("by-event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<EventContactLink>>> GetByEvent(int eventId)
    {
        var links = await repo.GetByEventIdAsync(eventId);
        return Ok(links);
    }

    [HttpGet("by-contact/{contactId:int}")]
    public async Task<ActionResult<IEnumerable<EventContactLink>>> GetByContact(int contactId)
    {
        var links = await repo.GetByContactIdAsync(contactId);
        return Ok(links);
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventContactLink link)
    {
        var created = await repo.CreateAsync(link);
        return created ? StatusCode(StatusCodes.Status201Created) : Conflict("Link already exists.");
    }

    [HttpDelete("{contactId:int}/{eventId:int}")]
    public async Task<IActionResult> Delete(int contactId, int eventId)
    {
        var deleted = await repo.DeleteAsync(contactId, eventId);
        return deleted ? NoContent() : NotFound();
    }
}
