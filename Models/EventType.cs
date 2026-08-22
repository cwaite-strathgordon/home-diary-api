using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public class EventType
{
    public int     EventTypeId  { get; set; }

    [Required]
    [StringLength(255)]
    public string? Title        { get; set; }

    [StringLength(500)]
    public string? Description  { get; set; }
}
