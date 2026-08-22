namespace HomeDiary_api.Models;

public class EventTaskSummary
{
    public long AllActiveTasks { get; set; }
    public long OverdueTasks { get; set; }
    public long DueNextSevenDays { get; set; }
    public long CriticalTasks { get; set; }
    public long CompletedLastMonth { get; set; }
    public long CreatedLastSevenDays { get; set; }
}
