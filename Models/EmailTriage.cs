using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace HomeDiary_api.Models;

public class EmailIntakeSummary
{
    public long EmailIntakeId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderMessageId { get; set; } = string.Empty;
    public string EnvelopeRecipient { get; set; } = string.Empty;
    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }
    public string? Subject { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TriageSummary { get; set; }
    public string? TriageReason { get; set; }
    public decimal? TriageConfidence { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public int SuggestionCount { get; set; }
    public int PendingSuggestionCount { get; set; }
    public string AiStatus { get; set; } = "pending";
    public DateTimeOffset? AiProcessedAt { get; set; }
}

public class EmailIntakeDetail : EmailIntakeSummary
{
    public string StorageBucket { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string? ReplyToEmail { get; set; }
    public JsonElement? AuthenticationResult { get; set; }
    public string? ExtractedText { get; set; }
    public JsonElement? ExtractionResult { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }
    public int? ReviewedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? AiLastError { get; set; }
    public IReadOnlyList<EmailTaskSuggestion> Suggestions { get; set; } = [];
    public IReadOnlyList<EmailAiRun> AiRuns { get; set; } = [];
}

public class EmailTaskSuggestion
{
    public long EmailTaskSuggestionId { get; set; }
    public long EmailIntakeId { get; set; }
    public int SuggestionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public int? EventTypeId { get; set; }
    public int? AreaId { get; set; }
    public int? PriorityId { get; set; }
    public int? ProjectId { get; set; }
    public decimal? Confidence { get; set; }
    public JsonElement MissingInformation { get; set; }
    public JsonElement? ExtractionData { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewNotes { get; set; }
    public int? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public int? EventId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string ActionType { get; set; } = "create_task";
    public int? TargetEventId { get; set; }
    public long? SourceAiRunId { get; set; }
    public string? Reason { get; set; }
    public JsonElement Evidence { get; set; }
    public JsonElement? ProposedChanges { get; set; }
}

public class EmailAiRun
{
    public long EmailAiRunId { get; set; }
    public long EmailIntakeId { get; set; }
    public string RunRole { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public JsonElement? Response { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class EmailTriagePage
{
    public required IReadOnlyList<EmailIntakeSummary> Items { get; set; }
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

public class EmailIntakeReviewRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? TriageSummary { get; set; }
    public string? TriageReason { get; set; }
}

public class EmailSuggestionReviewRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? ReviewNotes { get; set; }
    public int? EventTypeId { get; set; }
    public int? AreaId { get; set; }
    public int? PriorityId { get; set; }
    public int? ProjectId { get; set; }
}

public class EmailSuggestionReviewResult
{
    public string Status { get; set; } = string.Empty;
    public int? EventId { get; set; }
    public bool Applied { get; set; }
}

public class EmailSuggestionApplicationException(string message) : Exception(message);
