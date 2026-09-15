using System.Security.Claims;
using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Authorize]
[Route("api/application-parameters")]
public sealed class ApplicationParametersController(IApplicationParameterRepository repository)
    : ControllerBase
{
    [HttpGet("ai")]
    [Authorize(Policy = "HomeDiaryAdmin")]
    public async Task<ActionResult<AiSettings>> GetAiSettings() =>
        Ok(await repository.GetAiSettingsAsync());

    [HttpPut("ai")]
    [Authorize(Policy = "HomeDiaryAdmin")]
    public async Task<ActionResult<AiSettings>> UpdateAiSettings(UpdateAiSettingsRequest request)
    {
        if (request.ParallelEnabled &&
            request.PrimaryProvider.Equals(request.ParallelProvider, StringComparison.OrdinalIgnoreCase))
            return BadRequest("The parallel provider must differ from the primary provider.");

        return Ok(await repository.UpdateAiSettingsAsync(request, GetCurrentUserId()));
    }

    [HttpGet("application")]
    public async Task<ActionResult<ApplicationSettings>> GetApplicationSettings() =>
        Ok(await repository.GetApplicationSettingsAsync());

    [HttpPut("application")]
    [Authorize(Policy = "HomeDiaryAdmin")]
    public async Task<ActionResult<ApplicationSettings>> UpdateApplicationSettings(
        UpdateApplicationSettingsRequest request) =>
        Ok(await repository.UpdateApplicationSettingsAsync(request, GetCurrentUserId()));

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue("homediary_user_id");
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated HomeDiary user ID is missing.");
    }
}
