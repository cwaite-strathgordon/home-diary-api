using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/event-priorities")]
public class EventPriorityController(IEventPriorityRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventPriority>>> GetAll()
    {
        return Ok(await repo.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventPriority>> GetById(int id)
    {
        var priority = await repo.GetByIdAsync(id);
        return priority is null ? NotFound() : Ok(priority);
    }
}
