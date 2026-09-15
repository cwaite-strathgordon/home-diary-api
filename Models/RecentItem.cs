using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public sealed class RecentItem
{
    public long RecentItemViewId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ViewedAt { get; set; }
}

public sealed class ApplicationSettings
{
    public int RecentItemsLimit { get; set; } = 20;
    public string InboundEmailAddress { get; set; } = "tasks@homediary.app";
    public int MaximumImageUploadMegabytes { get; set; } = 3;
}

public sealed class UpdateApplicationSettingsRequest
{
    [Range(1, 100)]
    public int RecentItemsLimit { get; set; } = 20;

    [Required, EmailAddress]
    public string InboundEmailAddress { get; set; } = "tasks@homediary.app";

    [Range(1, 20)]
    public int MaximumImageUploadMegabytes { get; set; } = 3;
}
