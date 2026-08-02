using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public class HomeEvent
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
    public string? Title { get; set; }

    public string?   Description   { get; set; }
    public DateOnly? EventDate     { get; set; }
    public DateOnly? CreatedDate   { get; set; }
    public int?      CreatedById   { get; set; }
    public DateOnly? UpdatedDate   { get; set; }

    [Required(ErrorMessage = "EventTypeId is required.")]
    public int? EventTypeId { get; set; }

    [Required(ErrorMessage = "AreaId is required.")]
    public int? AreaId { get; set; }

    [Required(ErrorMessage = "EventStatusId is required.")]
    public int? EventStatusId { get; set; }
}
