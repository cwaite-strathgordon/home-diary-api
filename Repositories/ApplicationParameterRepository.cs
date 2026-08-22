using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public sealed class ApplicationParameterRepository(
    DbConnectionFactory db,
    ApplicationParameterProtector protector,
    ErrorLogRepository errorLog,
    ClientContext clientContext) : IApplicationParameterRepository
{
    public async Task<AiSettings> GetAiSettingsAsync()
    {
        try
        {
            using var conn = db.Create();
            var rows = await conn.QueryAsync<ParameterRow>(
                """
                SELECT parameter_key, parameter_value, parameter_type
                 FROM application_parameter
                 WHERE client_id = @clientId AND parameter_key LIKE 'ai.%'
                """, new { clientId = clientContext.RequireClientId() });
            return Map(rows);
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ApplicationParameterRepository));
            throw;
        }
    }

    public async Task<AiSettings> UpdateAiSettingsAsync(UpdateAiSettingsRequest request, int updatedById)
    {
        try
        {
            request.PrimaryProvider = NormaliseProvider(request.PrimaryProvider);
            request.ParallelProvider = NormaliseProvider(request.ParallelProvider);
            if (request.ParallelEnabled && request.PrimaryProvider == request.ParallelProvider)
                throw new ArgumentException("The parallel provider must differ from the primary provider.");

            using var conn = db.Create();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            await UpsertAsync(conn, transaction, "ai.enabled", request.Enabled ? "true" : "false", "boolean", updatedById);
            await UpsertAsync(conn, transaction, "ai.primary_provider", request.PrimaryProvider, "string", updatedById);
            await UpsertAsync(conn, transaction, "ai.parallel_enabled", request.ParallelEnabled ? "true" : "false", "boolean", updatedById);
            await UpsertAsync(conn, transaction, "ai.parallel_provider", request.ParallelProvider, "string", updatedById);
            await UpsertAsync(conn, transaction, "ai.openai.model", request.OpenAiModel.Trim(), "string", updatedById);
            await UpsertAsync(conn, transaction, "ai.deepseek.model", request.DeepSeekModel.Trim(), "string", updatedById);

            await UpdateSecretAsync(
                conn, transaction, "ai.openai.api_key", request.OpenAiApiKey,
                request.ClearOpenAiApiKey, updatedById);
            await UpdateSecretAsync(
                conn, transaction, "ai.deepseek.api_key", request.DeepSeekApiKey,
                request.ClearDeepSeekApiKey, updatedById);

            transaction.Commit();
            return await GetAiSettingsAsync();
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ApplicationParameterRepository));
            throw;
        }
    }

    public async Task<ApplicationSettings> GetApplicationSettingsAsync()
    {
        try
        {
            using var conn = db.Create();
            var raw = await conn.QuerySingleOrDefaultAsync<string>(
                """
                SELECT parameter_value
                  FROM application_parameter
                 WHERE client_id=@clientId AND parameter_key='recent_items.limit'
                """, new { clientId = clientContext.RequireClientId() });
            return new ApplicationSettings
            {
                RecentItemsLimit = int.TryParse(raw, out var value) ? Math.Clamp(value, 1, 100) : 20
            };
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ApplicationParameterRepository));
            throw;
        }
    }

    public async Task<ApplicationSettings> UpdateApplicationSettingsAsync(
        UpdateApplicationSettingsRequest request,
        int updatedById)
    {
        try
        {
            var limit = Math.Clamp(request.RecentItemsLimit, 1, 100);
            using var conn = db.Create();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            await UpsertAsync(conn, transaction, "recent_items.limit", limit.ToString(), "integer", updatedById);
            await conn.ExecuteAsync(
                """
                DELETE FROM recent_item_view
                 WHERE client_id=@clientId
                   AND recent_item_view_id IN (
                       SELECT recent_item_view_id
                         FROM (
                           SELECT recent_item_view_id,
                                  row_number() OVER (
                                      PARTITION BY user_id
                                      ORDER BY viewed_at DESC, recent_item_view_id DESC) AS position
                             FROM recent_item_view
                            WHERE client_id=@clientId
                         ) ranked
                        WHERE position > @limit)
                """, new { clientId = clientContext.RequireClientId(), limit }, transaction);
            transaction.Commit();
            return new ApplicationSettings { RecentItemsLimit = limit };
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(ApplicationParameterRepository));
            throw;
        }
    }

    private async Task UpdateSecretAsync(
        System.Data.IDbConnection conn,
        System.Data.IDbTransaction? transaction,
        string key,
        string? newValue,
        bool clear,
        int updatedById)
    {
        if (clear)
        {
            await UpsertAsync(conn, transaction, key, null, "secret", updatedById);
            return;
        }

        if (!string.IsNullOrWhiteSpace(newValue))
            await UpsertAsync(conn, transaction, key, protector.Protect(newValue.Trim()), "secret", updatedById);
    }

    private Task<int> UpsertAsync(
        System.Data.IDbConnection conn,
        System.Data.IDbTransaction? transaction,
        string key,
        string? value,
        string type,
        int updatedById) =>
        conn.ExecuteAsync(
            """
            INSERT INTO application_parameter
                   (client_id, parameter_key, parameter_value, parameter_type, updated_by_id)
            VALUES (@clientId, @key, @value, @type, @updatedById)
            ON CONFLICT (client_id, parameter_key) DO UPDATE
               SET parameter_value = EXCLUDED.parameter_value,
                   parameter_type = EXCLUDED.parameter_type,
                   updated_by_id = EXCLUDED.updated_by_id,
                   updated_at = now()
            """, new { clientId = clientContext.RequireClientId(), key, value, type, updatedById }, transaction);

    private static AiSettings Map(IEnumerable<ParameterRow> rows)
    {
        var values = rows.ToDictionary(row => row.ParameterKey, StringComparer.OrdinalIgnoreCase);
        string Value(string key, string fallback) =>
            values.TryGetValue(key, out var row) && !string.IsNullOrWhiteSpace(row.ParameterValue)
                ? row.ParameterValue
                : fallback;
        bool Flag(string key, bool fallback = false) =>
            bool.TryParse(Value(key, fallback.ToString()), out var value) ? value : fallback;
        bool Configured(string key) =>
            values.TryGetValue(key, out var row) && !string.IsNullOrWhiteSpace(row.ParameterValue);

        return new AiSettings
        {
            Enabled = Flag("ai.enabled"),
            PrimaryProvider = Value("ai.primary_provider", "openai"),
            ParallelEnabled = Flag("ai.parallel_enabled"),
            ParallelProvider = Value("ai.parallel_provider", "deepseek"),
            OpenAiModel = Value("ai.openai.model", "gpt-5.6-sol"),
            DeepSeekModel = Value("ai.deepseek.model", "deepseek-v4-flash"),
            OpenAiApiKeyConfigured = Configured("ai.openai.api_key"),
            DeepSeekApiKeyConfigured = Configured("ai.deepseek.api_key")
        };
    }

    private static string NormaliseProvider(string value) => value.Trim().ToLowerInvariant();

    private sealed class ParameterRow
    {
        public string ParameterKey { get; set; } = string.Empty;
        public string? ParameterValue { get; set; }
        public string ParameterType { get; set; } = string.Empty;
    }
}
