namespace HomeDiary_api.Models;

public class GlobalSearchResult
{
    public string ResultType { get; set; } = string.Empty;
    public int ObjectId { get; set; }
    public int? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string SearchSnippet { get; set; } = string.Empty;
    public float Rank { get; set; }
}
