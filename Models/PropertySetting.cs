using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public class PropertySetting
{
    public int SettingId { get; set; }
    public int ClientId { get; set; }
    [Required, MaxLength(255)] public string PropertyName { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string AddressLine1 { get; set; } = string.Empty;
    [MaxLength(255)] public string? AddressLine2 { get; set; }
    [Required, MaxLength(120)] public string City { get; set; } = string.Empty;
    [MaxLength(120)] public string? Region { get; set; }
    [Required, MaxLength(30)] public string Postcode { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Country { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? GeocodedAddress { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
    public int? UpdatedById { get; set; }
    [MaxLength(80)] public string? PropertyType { get; set; }
    public int? ConstructionYear { get; set; }
    public short? BedroomCount { get; set; }
    public short? BathroomCount { get; set; }
    public bool HasGarden { get; set; }
    public bool HasGarage { get; set; }
    public bool HasAirConditioning { get; set; }
    public bool HasGasBoiler { get; set; }
    public bool HasSolarPanels { get; set; }
    public bool HasPool { get; set; }
}
