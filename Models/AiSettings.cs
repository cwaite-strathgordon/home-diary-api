using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public sealed class AiSettings
{
    public bool Enabled { get; set; }
    public string PrimaryProvider { get; set; } = "openai";
    public bool ParallelEnabled { get; set; }
    public string ParallelProvider { get; set; } = "deepseek";
    public string OpenAiModel { get; set; } = "gpt-5.6-sol";
    public string DeepSeekModel { get; set; } = "deepseek-v4-flash";
    public bool OpenAiApiKeyConfigured { get; set; }
    public bool DeepSeekApiKeyConfigured { get; set; }
}

public sealed class UpdateAiSettingsRequest
{
    public bool Enabled { get; set; }

    [Required, RegularExpression("^(openai|deepseek)$")]
    public string PrimaryProvider { get; set; } = "openai";

    public bool ParallelEnabled { get; set; }

    [Required, RegularExpression("^(openai|deepseek)$")]
    public string ParallelProvider { get; set; } = "deepseek";

    [Required, MaxLength(100)]
    public string OpenAiModel { get; set; } = "gpt-5.6-sol";

    [Required, MaxLength(100)]
    public string DeepSeekModel { get; set; } = "deepseek-v4-flash";

    [MaxLength(500)]
    public string? OpenAiApiKey { get; set; }

    [MaxLength(500)]
    public string? DeepSeekApiKey { get; set; }

    public bool ClearOpenAiApiKey { get; set; }
    public bool ClearDeepSeekApiKey { get; set; }
}
