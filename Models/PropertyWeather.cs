namespace HomeDiary_api.Models;

public class PropertyWeather
{
    public string PropertyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double ApparentTemperature { get; set; }
    public int RelativeHumidity { get; set; }
    public int WeatherCode { get; set; }
    public bool IsDay { get; set; }
    public double WindSpeed { get; set; }
    public double MaximumTemperature { get; set; }
    public double MinimumTemperature { get; set; }
    public int? PrecipitationProbability { get; set; }
    public string? Time { get; set; }
    public List<PropertyWeatherDay> Forecast { get; set; } = [];
}

public class PropertyWeatherDay
{
    public string Date { get; set; } = string.Empty;
    public int WeatherCode { get; set; }
    public double MaximumTemperature { get; set; }
    public double MinimumTemperature { get; set; }
    public int? PrecipitationProbability { get; set; }
}
