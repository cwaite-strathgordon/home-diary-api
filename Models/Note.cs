namespace HomeDiary_api.Models;

public class Note
{
    public int NoteId { get; set; }
    public int LinkObjectTypeId { get; set; }
    public int LinkObjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
    public DateTimeOffset? CreatedDate { get; set; }
    public int? CreatedById { get; set; }
    public string? CreatedByFirstName { get; set; }
    public string? CreatedByLastName { get; set; }
    public string? CreatedByEmail { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
    public int? UpdatedById { get; set; }
}
