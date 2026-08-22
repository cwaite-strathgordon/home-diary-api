namespace HomeDiary_api.Models;

public class EventDocument
{
    public int EventDocumentId { get; set; }
    public int EventId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public byte[] FileData { get; set; } = [];
    public string ExtractedText { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public int? CreatedById { get; set; }
    public string? SearchSnippet { get; set; }
    public string? EventTitle { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
}
