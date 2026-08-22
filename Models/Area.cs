using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public class Area
{
    public int     AreaId      { get; set; }

    [Required]
    [StringLength(255)]
    public string? Title       { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
