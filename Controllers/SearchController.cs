using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(IGlobalSearchRepository search) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GlobalSearchResult>>> Get([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return BadRequest("Enter at least two characters.");
        return Ok(await search.SearchAsync(query.Trim()));
    }
}
