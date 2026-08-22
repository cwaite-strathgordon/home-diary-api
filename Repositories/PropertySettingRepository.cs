using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class PropertySettingRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext)
    : IPropertySettingRepository
{
    public async Task<PropertySetting?> GetAsync()
    {
        try
        {
            using var conn = db.Create();
            return await conn.QuerySingleOrDefaultAsync<PropertySetting>(
                """
                SELECT setting_id, property_name, address_line_1, address_line_2, city,
                       region, postcode, country, latitude, longitude, geocoded_address,
                       updated_date, updated_by_id, client_id, property_type, construction_year,
                       bedroom_count, bathroom_count, has_garden, has_garage,
                       has_air_conditioning, has_gas_boiler, has_solar_panels, has_pool
                  FROM property_setting
                 WHERE client_id = @clientId
                """, new { clientId = clientContext.RequireClientId() });
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(PropertySettingRepository));
            throw;
        }
    }

    public async Task<PropertySetting> SaveAsync(PropertySetting setting)
    {
        try
        {
            using var conn = db.Create();
            await conn.ExecuteAsync(
                """
                INSERT INTO property_setting
                       (client_id, property_name, address_line_1, address_line_2, city,
                        region, postcode, country, latitude, longitude, geocoded_address, updated_by_id,
                        property_type, construction_year, bedroom_count, bathroom_count,
                        has_garden, has_garage, has_air_conditioning, has_gas_boiler, has_solar_panels, has_pool)
                VALUES (@ClientId, @PropertyName, @AddressLine1, @AddressLine2, @City,
                        @Region, @Postcode, @Country, @Latitude, @Longitude, @GeocodedAddress, @UpdatedById,
                        @PropertyType, @ConstructionYear, @BedroomCount, @BathroomCount,
                        @HasGarden, @HasGarage, @HasAirConditioning, @HasGasBoiler, @HasSolarPanels, @HasPool)
                ON CONFLICT (client_id) DO UPDATE SET
                    property_name = EXCLUDED.property_name,
                    address_line_1 = EXCLUDED.address_line_1,
                    address_line_2 = EXCLUDED.address_line_2,
                    city = EXCLUDED.city,
                    region = EXCLUDED.region,
                    postcode = EXCLUDED.postcode,
                    country = EXCLUDED.country,
                    latitude = EXCLUDED.latitude,
                    longitude = EXCLUDED.longitude,
                    geocoded_address = EXCLUDED.geocoded_address,
                    updated_date = now(),
                    updated_by_id = EXCLUDED.updated_by_id,
                    property_type = EXCLUDED.property_type,
                    construction_year = EXCLUDED.construction_year,
                    bedroom_count = EXCLUDED.bedroom_count,
                    bathroom_count = EXCLUDED.bathroom_count,
                    has_garden = EXCLUDED.has_garden,
                    has_garage = EXCLUDED.has_garage,
                    has_air_conditioning = EXCLUDED.has_air_conditioning,
                    has_gas_boiler = EXCLUDED.has_gas_boiler,
                    has_solar_panels = EXCLUDED.has_solar_panels,
                    has_pool = EXCLUDED.has_pool
                """, WithClient(setting));
            return (await GetAsync())!;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(PropertySettingRepository));
            throw;
        }
    }

    private object WithClient(PropertySetting s) => new
    {
        ClientId = clientContext.RequireClientId(), s.PropertyName, s.AddressLine1, s.AddressLine2,
        s.City, s.Region, s.Postcode, s.Country, s.Latitude, s.Longitude, s.GeocodedAddress,
        s.UpdatedById, s.PropertyType, s.ConstructionYear, s.BedroomCount, s.BathroomCount,
        s.HasGarden, s.HasGarage, s.HasAirConditioning, s.HasGasBoiler, s.HasSolarPanels, s.HasPool
    };
}
