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

    // Statuses are immutable system reference data. Allowing a client
    // administrator to edit them would change behaviour for every tenant.
}
