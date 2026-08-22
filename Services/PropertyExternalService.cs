using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HomeDiary_api.Models;

namespace HomeDiary_api.Services;

public class PropertyExternalService(
    HttpClient httpClient,
    ILogger<PropertyExternalService> logger)
{
    private static readonly TimeSpan NominatimRetryDelay = TimeSpan.FromMilliseconds(1100);

    public async Task<(decimal Latitude, decimal Longitude, string DisplayName)?> GeocodeAsync(
        PropertySetting setting, CancellationToken cancellationToken = default)
    {
        var fullAddress = JoinAddress(
            setting.AddressLine1, setting.AddressLine2, setting.City,
            setting.Region, setting.Postcode, setting.Country);

        var result = await SearchAddressAsync(fullAddress, cancellationToken);
        if (result is null)
        {
            // Nominatim's free-text search can reject an otherwise usable address when
            // the street is misspelled or is not present in OpenStreetMap. Onboarding
            // only needs reliable coordinates for local weather, so fall back to the
            // postcode and town rather than blocking creation of the client.
            var postcodeAddress = JoinAddress(setting.Postcode, setting.City, setting.Country);
            if (!string.Equals(postcodeAddress, fullAddress, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation(
                    "Exact property address was not found; retrying geocoding using postcode and city");
                await Task.Delay(NominatimRetryDelay, cancellationToken);
                result = await SearchAddressAsync(postcodeAddress, cancellationToken);
            }
        }

        if (result is null) return null;
        return (result.Value.Latitude, result.Value.Longitude, result.Value.DisplayName);
    }

    private async Task<(decimal Latitude, decimal Longitude, string DisplayName)?> SearchAddressAsync(
        string address, CancellationToken cancellationToken)
    {
        var url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q="
            + Uri.EscapeDataString(address);
        var results = await httpClient.GetFromJsonAsync<List<NominatimResult>>(url, cancellationToken);
        var result = results?.FirstOrDefault();
        if (result is null
            || !decimal.TryParse(result.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            || !decimal.TryParse(result.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            return null;
        return (latitude, longitude, result.DisplayName ?? address);
    }

    private static string JoinAddress(params string?[] values) =>
        string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));

    public async Task<PropertyWeather?> GetWeatherAsync(
        PropertySetting setting, CancellationToken cancellationToken = default)
    {
        var latitude = setting.Latitude.ToString(CultureInfo.InvariantCulture);
        var longitude = setting.Longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}"
            + "&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,is_day,wind_speed_10m"
            + "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&forecast_days=7&timezone=auto";
        var response = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken);
        if (response?.Current is null) return null;
        var forecast = response.Daily is null ? [] : Enumerable.Range(0, response.Daily.Time.Count)
            .Select(index => new PropertyWeatherDay
            {
                Date = response.Daily.Time[index],
                WeatherCode = response.Daily.WeatherCode.ElementAtOrDefault(index),
                MaximumTemperature = response.Daily.MaximumTemperature.ElementAtOrDefault(index),
                MinimumTemperature = response.Daily.MinimumTemperature.ElementAtOrDefault(index),
                PrecipitationProbability = response.Daily.PrecipitationProbability.ElementAtOrDefault(index),
            }).ToList();
        return new PropertyWeather
        {
            PropertyName = setting.PropertyName,
            Location = string.Join(", ", new[] { setting.City, setting.Country }.Where(value => !string.IsNullOrWhiteSpace(value))),
            Temperature = response.Current.Temperature,
            ApparentTemperature = response.Current.ApparentTemperature,
            RelativeHumidity = response.Current.RelativeHumidity,
            WeatherCode = response.Current.WeatherCode,
            IsDay = response.Current.IsDay == 1,
            WindSpeed = response.Current.WindSpeed,
            MaximumTemperature = response.Daily?.MaximumTemperature.FirstOrDefault() ?? response.Current.Temperature,
            MinimumTemperature = response.Daily?.MinimumTemperature.FirstOrDefault() ?? response.Current.Temperature,
            PrecipitationProbability = response.Daily?.PrecipitationProbability.FirstOrDefault(),
            Time = response.Current.Time,
            Forecast = forecast,
        };
    }

    private sealed class NominatimResult
    {
        [JsonPropertyName("lat")] public string Latitude { get; set; } = string.Empty;
        [JsonPropertyName("lon")] public string Longitude { get; set; } = string.Empty;
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")] public CurrentWeather? Current { get; set; }
        [JsonPropertyName("daily")] public DailyWeather? Daily { get; set; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("time")] public string? Time { get; set; }
        [JsonPropertyName("temperature_2m")] public double Temperature { get; set; }
        [JsonPropertyName("apparent_temperature")] public double ApparentTemperature { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public int RelativeHumidity { get; set; }
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
        [JsonPropertyName("is_day")] public int IsDay { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double WindSpeed { get; set; }
    }

    private sealed class DailyWeather
    {
        [JsonPropertyName("time")] public List<string> Time { get; set; } = [];
        [JsonPropertyName("weather_code")] public List<int> WeatherCode { get; set; } = [];
        [JsonPropertyName("temperature_2m_max")] public List<double> MaximumTemperature { get; set; } = [];
        [JsonPropertyName("temperature_2m_min")] public List<double> MinimumTemperature { get; set; } = [];
        [JsonPropertyName("precipitation_probability_max")] public List<int?> PrecipitationProbability { get; set; } = [];
    }
}
