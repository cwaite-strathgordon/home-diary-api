using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using HomeDiary_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/property-settings")]
public class PropertySettingsController(
    IPropertySettingRepository repository,
    PropertyExternalService externalService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PropertySetting?>> Get()
    {
        return Ok(await repository.GetAsync());
    }

    [HttpPut]
    public async Task<ActionResult<PropertySetting>> Save(PropertySetting setting, CancellationToken cancellationToken)
    {
        setting.PropertyName = setting.PropertyName.Trim();
        setting.AddressLine1 = setting.AddressLine1.Trim();
        setting.AddressLine2 = setting.AddressLine2?.Trim();
        setting.City = setting.City.Trim();
        setting.Region = setting.Region?.Trim();
        setting.Postcode = setting.Postcode.Trim();
        setting.Country = setting.Country.Trim();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var location = await externalService.GeocodeAsync(setting, cancellationToken);
        if (location is null)
            return BadRequest("The address could not be located. Check the address and postcode, then try again.");

        setting.Latitude = location.Value.Latitude;
        setting.Longitude = location.Value.Longitude;
        setting.GeocodedAddress = location.Value.DisplayName;
        setting.UpdatedById = CurrentUserId();
        return Ok(await repository.SaveAsync(setting));
    }

    [HttpGet("weather")]
    public async Task<ActionResult<PropertyWeather>> GetWeather(CancellationToken cancellationToken)
    {
        var setting = await repository.GetAsync();
        if (setting is null) return NotFound("Property details have not been configured.");
        var weather = await externalService.GetWeatherAsync(setting, cancellationToken);
        return weather is null ? StatusCode(502, "Weather information is currently unavailable.") : Ok(weather);
    }

    private int CurrentUserId()
    {
        return int.TryParse(User.FindFirst("homediary_user_id")?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated HomeDiary user ID is missing.");
    }
}
