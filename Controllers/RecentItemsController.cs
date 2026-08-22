using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/recent-items")]
public sealed class RecentItemsController(IRecentItemRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecentItem>>> Get() =>
        Ok(await repository.GetAsync(CurrentUserId()));

    [HttpPost("{itemType}/{itemId:int}")]
    public async Task<ActionResult<RecentItem>> Record(string itemType, int itemId)
    {
        if (itemId <= 0) return BadRequest("The item ID must be positive.");
        if (itemType.Trim().ToLowerInvariant() is not ("task" or "project" or "contact"))
            return BadRequest("Recent item type must be task, project, or contact.");

        var item = await repository.RecordAsync(itemType, itemId, CurrentUserId());
        return item is null ? NotFound() : Ok(item);
    }

    private int CurrentUserId() => int.TryParse(
        User.FindFirst("homediary_user_id")?.Value, out var id)
        ? id : throw new InvalidOperationException("Authenticated user ID is missing.");
}
