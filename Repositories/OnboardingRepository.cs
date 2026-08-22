using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using Npgsql;

namespace HomeDiary_api.Repositories;

public sealed class OnboardingRepository(DbConnectionFactory db) : IOnboardingRepository
{
    public async Task<OnboardingSuggestions> GetSuggestionsAsync(OnboardingSuggestionRequest request)
    {
        var features = Features(request);
        using var conn = db.Create();
        var areas = (await conn.QueryAsync<OnboardingAreaSuggestion>(
            """
            WITH candidates AS
            (
                SELECT title, description, sort_order
                  FROM onboarding_area_template
                 WHERE (property_type IS NULL OR lower(property_type) = lower(@PropertyType))
                   AND (feature_key IS NULL OR feature_key = ANY(@Features))
                UNION ALL
                SELECT min(title), NULL::varchar, 500 + count(*)::integer
                  FROM area
                 GROUP BY lower(title)
                HAVING count(DISTINCT client_id) >= 3
            )
            SELECT min(title) AS title, max(description) AS description, true AS selected
              FROM candidates GROUP BY lower(title)
             ORDER BY min(sort_order), min(title)
            """, new { request.PropertyType, Features = features })).AsList();
        var tasks = (await conn.QueryAsync<OnboardingMaintenanceSuggestion>(
            """
            WITH candidates AS
            (
                SELECT title, description, recurrence_unit, recurrence_interval,
                       suggested_area, sort_order
                  FROM onboarding_maintenance_template
                 WHERE (property_type IS NULL OR lower(property_type) = lower(@PropertyType))
                   AND (feature_key IS NULL OR feature_key = ANY(@Features))
                UNION ALL
                SELECT min(he.title), NULL::text, he.recurrence_unit, he.recurrence_interval,
                       min(a.title), 500 + count(*)::integer
                  FROM home_event he LEFT JOIN area a ON a.area_id=he.area_id
                 WHERE he.is_recurring AND he.recurrence_interval IS NOT NULL
                 GROUP BY lower(he.title), he.recurrence_unit, he.recurrence_interval
                HAVING count(DISTINCT he.client_id) >= 3
            )
            SELECT min(title) AS title, max(description) AS description,
                   recurrence_unit, recurrence_interval, max(suggested_area) AS suggested_area,
                   true AS selected
              FROM candidates
             GROUP BY lower(title), recurrence_unit, recurrence_interval
             ORDER BY min(sort_order), min(title)
            """, new { request.PropertyType, Features = features })).AsList();
        return new OnboardingSuggestions { Areas = areas, MaintenanceTasks = tasks };
    }

    public async Task<User> CompleteAsync(
        int userId, CompleteOnboardingRequest request, CancellationToken cancellationToken)
    {
        await using var conn = (NpgsqlConnection)db.Create();
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        var current = await conn.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            "SELECT user_id, client_id FROM app_user WHERE user_id = @userId FOR UPDATE",
            new { userId }, transaction, cancellationToken: cancellationToken));
        if (current is null) throw new InvalidOperationException("The signed-in user no longer exists.");
        if (current.ClientId is not null) throw new InvalidOperationException("Onboarding has already been completed.");

        var clientId = await conn.QuerySingleAsync<int>(new CommandDefinition(
            "INSERT INTO client(name) VALUES (@name) RETURNING client_id",
            new { name = request.ClientName.Trim() }, transaction, cancellationToken: cancellationToken));
        var inboundEmailAddress = $"tasks-{clientId}@tasks.homediary.app";
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE client SET inbound_email_address=@inboundEmailAddress WHERE client_id=@clientId",
            new { clientId, inboundEmailAddress }, transaction, cancellationToken: cancellationToken));

        await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE app_user
               SET client_id = @clientId, first_name = @firstName, last_name = @lastName,
                   email = @email, mobile_number = @mobileNumber, admin = true,
                   disabled = false, updated_at = now()
             WHERE user_id = @userId AND client_id IS NULL
            """, new
            {
                clientId, userId,
                firstName = request.FirstName.Trim(), lastName = request.LastName.Trim(),
                email = request.Email.Trim(), mobileNumber = Clean(request.MobileNumber)
            }, transaction, cancellationToken: cancellationToken));

        var p = request.Property;
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO property_setting
                   (client_id, property_name, address_line_1, address_line_2, city, region,
                    postcode, country, latitude, longitude, geocoded_address, updated_by_id,
                    property_type, construction_year, bedroom_count, bathroom_count,
                    has_garden, has_garage, has_air_conditioning, has_gas_boiler,
                    has_solar_panels, has_pool)
            VALUES (@clientId, @PropertyName, @AddressLine1, @AddressLine2, @City, @Region,
                    @Postcode, @Country, @Latitude, @Longitude, @GeocodedAddress, @userId,
                    @PropertyType, @ConstructionYear, @BedroomCount, @BathroomCount,
                    @HasGarden, @HasGarage, @HasAirConditioning, @HasGasBoiler,
                    @HasSolarPanels, @HasPool)
            """, new
            {
                clientId, userId, p.PropertyName, p.AddressLine1, p.AddressLine2, p.City, p.Region,
                p.Postcode, p.Country, p.Latitude, p.Longitude, p.GeocodedAddress, p.PropertyType,
                p.ConstructionYear, p.BedroomCount, p.BathroomCount, p.HasGarden, p.HasGarage,
                p.HasAirConditioning, p.HasGasBoiler, p.HasSolarPanels, p.HasPool
            }, transaction, cancellationToken: cancellationToken));

        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO event_type(client_id, title, description)
            VALUES (@clientId, 'Maintenance', 'Planned and reactive property maintenance'),
                   (@clientId, 'Inspection', 'Checks and inspections'),
                   (@clientId, 'Administration', 'Property administration and records')
            ON CONFLICT DO NOTHING
            """, new { clientId }, transaction, cancellationToken: cancellationToken));

        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO application_parameter(client_id, parameter_key, parameter_value, parameter_type, description)
            VALUES
              (@clientId, 'ai.enabled', 'false', 'boolean', 'Enable automatic AI email triage.'),
              (@clientId, 'ai.primary_provider', 'openai', 'string', 'Primary AI provider.'),
              (@clientId, 'ai.parallel_enabled', 'false', 'boolean', 'Enable shadow provider comparison.'),
              (@clientId, 'ai.parallel_provider', 'deepseek', 'string', 'Shadow AI provider.'),
              (@clientId, 'ai.openai.model', 'gpt-5.6-sol', 'string', 'OpenAI model.'),
              (@clientId, 'ai.deepseek.model', 'deepseek-v4-flash', 'string', 'DeepSeek model.'),
              (@clientId, 'ai.openai.api_key', NULL, 'secret', 'Encrypted OpenAI API key.'),
              (@clientId, 'ai.deepseek.api_key', NULL, 'secret', 'Encrypted DeepSeek API key.')
            ON CONFLICT DO NOTHING
            """, new { clientId }, transaction, cancellationToken: cancellationToken));

        foreach (var area in request.Areas.Where(x => x.Selected && !string.IsNullOrWhiteSpace(x.Title))
                     .DistinctBy(x => x.Title.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO area(client_id, title, description) VALUES (@clientId, @title, @description) ON CONFLICT DO NOTHING",
                new { clientId, title = area.Title.Trim(), description = Clean(area.Description) },
                transaction, cancellationToken: cancellationToken));
        }

        var maintenanceTypeId = await conn.QuerySingleAsync<int>(new CommandDefinition(
            "SELECT event_type_id FROM event_type WHERE client_id=@clientId AND lower(title)='maintenance'",
            new { clientId }, transaction, cancellationToken: cancellationToken));
        var pendingStatusId = await conn.QuerySingleAsync<int>(new CommandDefinition(
            "SELECT event_status_id FROM event_status WHERE lower(title)='pending' LIMIT 1",
            transaction: transaction, cancellationToken: cancellationToken));
        var mediumPriorityId = await conn.QuerySingleAsync<int>(new CommandDefinition(
            "SELECT event_priority_id FROM event_priority WHERE lower(title)='medium' LIMIT 1",
            transaction: transaction, cancellationToken: cancellationToken));

        foreach (var task in request.MaintenanceTasks.Where(x => x.Selected && !string.IsNullOrWhiteSpace(x.Title)))
        {
            var areaId = await conn.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
                "SELECT area_id FROM area WHERE client_id=@clientId AND lower(title)=lower(@area) LIMIT 1",
                new { clientId, area = task.SuggestedArea }, transaction, cancellationToken: cancellationToken));
            await conn.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO home_event
                       (client_id, title, description, created_date, created_by_id,
                        event_type_id, area_id, event_status_id, priority_id,
                        target_completion_date, is_recurring, recurrence_interval, recurrence_unit)
                VALUES (@clientId, @title, @description, CURRENT_DATE, @userId,
                        @maintenanceTypeId, @areaId, @pendingStatusId, @mediumPriorityId,
                        CURRENT_DATE + 30, true, @recurrenceInterval, @recurrenceUnit)
                """, new
                {
                    clientId, userId, title = task.Title.Trim(), description = Clean(task.Description),
                    maintenanceTypeId, areaId, pendingStatusId, mediumPriorityId,
                    recurrenceInterval = Math.Max(1, task.RecurrenceInterval),
                    recurrenceUnit = NormaliseUnit(task.RecurrenceUnit)
                }, transaction, cancellationToken: cancellationToken));
        }

        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE client SET onboarding_completed_at=now(), updated_at=now() WHERE client_id=@clientId",
            new { clientId }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);

        return new User
        {
            UserId = userId, ClientId = clientId, ClientName = request.ClientName.Trim(),
            InboundEmailAddress = inboundEmailAddress,
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            Email = request.Email.Trim(), MobileNumber = Clean(request.MobileNumber), Admin = true
        };
    }

    private static string[] Features(OnboardingSuggestionRequest r) =>
        new[]
        {
            r.HasGarden ? "garden" : null, r.HasGarage ? "garage" : null,
            r.HasAirConditioning ? "air_conditioning" : null,
            r.HasGasBoiler ? "gas_boiler" : null, r.HasSolarPanels ? "solar_panels" : null,
            r.HasPool ? "pool" : null
        }.Where(x => x is not null).Cast<string>().ToArray();

    private static string NormaliseUnit(string? value) => value?.ToLowerInvariant() switch
    {
        "day" or "days" => "day",
        "week" or "weeks" => "week",
        "month" or "months" => "month",
        "year" or "years" => "year",
        _ => "months"
    };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
