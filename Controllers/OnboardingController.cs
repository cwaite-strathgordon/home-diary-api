using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using HomeDiary_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/onboarding")]
public sealed class OnboardingController(
    IOnboardingRepository repository,
    PropertyExternalService propertyExternalService) : ControllerBase
{
    [HttpPost("suggestions")]
    public Task<OnboardingSuggestions> Suggestions(OnboardingSuggestionRequest request) =>
        repository.GetSuggestionsAsync(request);

    [HttpPost("complete")]
    public async Task<ActionResult<User>> Complete(
        CompleteOnboardingRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("homediary_user_id")?.Value, out var userId)) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!request.Areas.Any(area => area.Selected && !string.IsNullOrWhiteSpace(area.Title)))
            return BadRequest(new ProblemDetails
            {
                Title = "Select at least one area",
                Detail = "HomeDiary tasks require an area. Keep at least one suggested area selected."
            });

        var location = await propertyExternalService.GeocodeAsync(request.Property, cancellationToken);
        if (location is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Property address not found",
                Detail = "Check the property address and postcode, then try again."
            });
        request.Property.Latitude = location.Value.Latitude;
        request.Property.Longitude = location.Value.Longitude;
        request.Property.GeocodedAddress = location.Value.DisplayName;

        try
        {
            return Ok(await repository.CompleteAsync(userId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Onboarding could not be completed", Detail = ex.Message });
        }
    }
}
