using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Models;

public class Project
{
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
    public string? Title { get; set; }

    public string? Description { get; set; }
    public int? AreaId { get; set; }
    public string? AreaTitle { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public DateOnly? CreatedDate { get; set; }
    public int? CreatedById { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public string Status { get; set; } = "Active";
    public DateOnly? ArchivedDate { get; set; }
    public int TotalTasks { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
}
