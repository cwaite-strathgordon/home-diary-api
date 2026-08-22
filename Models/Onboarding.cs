using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public sealed class OnboardingSuggestionRequest
{
    [MaxLength(80)] public string? PropertyType { get; set; }
    public bool HasGarden { get; set; }
    public bool HasGarage { get; set; }
    public bool HasAirConditioning { get; set; }
    public bool HasGasBoiler { get; set; }
    public bool HasSolarPanels { get; set; }
    public bool HasPool { get; set; }
}

public sealed class OnboardingSuggestions
{
    public IReadOnlyList<OnboardingAreaSuggestion> Areas { get; init; } = [];
    public IReadOnlyList<OnboardingMaintenanceSuggestion> MaintenanceTasks { get; init; } = [];
}

public sealed class OnboardingAreaSuggestion
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Selected { get; init; } = true;
}

public sealed class OnboardingMaintenanceSuggestion
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string RecurrenceUnit { get; init; } = "month";
    public int RecurrenceInterval { get; init; } = 1;
    public string? SuggestedArea { get; init; }
    public bool Selected { get; init; } = true;
}

public sealed class CompleteOnboardingRequest
{
    [Required, MaxLength(255)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(320)] public string Email { get; set; } = string.Empty;
    [MaxLength(50)] public string? MobileNumber { get; set; }
    [Required, MaxLength(255)] public string ClientName { get; set; } = string.Empty;
    [Required] public PropertySetting Property { get; set; } = new();
    public List<OnboardingAreaSuggestion> Areas { get; set; } = [];
    public List<OnboardingMaintenanceSuggestion> MaintenanceTasks { get; set; } = [];
}

public sealed class ClientInvitation
{
    public long ClientInvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Admin { get; set; }
    public Guid InvitationToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? SesMessageId { get; set; }
    public string? DeliveryError { get; set; }
}
