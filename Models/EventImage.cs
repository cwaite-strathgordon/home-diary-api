using System.Text.Json.Serialization;

namespace HomeDiary_api.Models;

public class EventImage
{
    public int EventImageId { get; set; }
    public int EventId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    [JsonIgnore]
    public byte[] ImageData { get; set; } = [];
    public DateTimeOffset CreatedDate { get; set; }
    public int? CreatedById { get; set; }
}
