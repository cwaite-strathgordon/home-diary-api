namespace HomeDiary_api.Models;

public class HomeEventFilter
{
    public string?   TitleContains       { get; set; }
    public string?   DescriptionContains { get; set; }
    public int?      EventTypeId         { get; set; }
    public int?      AreaId              { get; set; }
    public int?      EventStatusId       { get; set; }
    public List<int>? EventStatusIds     { get; set; }
    public int?      PriorityId          { get; set; }
    public int?      ProjectId           { get; set; }
    public int?      CreatedById         { get; set; }
    public DateOnly? EventDateFrom       { get; set; }
    public DateOnly? EventDateTo         { get; set; }
    public DateOnly? TargetCompletionDateFrom { get; set; }
    public DateOnly? TargetCompletionDateTo   { get; set; }
    public DateOnly? ActualCompletionDateFrom { get; set; }
    public DateOnly? ActualCompletionDateTo   { get; set; }
    public bool?     Overdue             { get; set; }
    public bool?     ActiveOnly          { get; set; }
    public bool?     ExcludeWishList     { get; set; }
    public bool?     RecurringOnly       { get; set; }
    public DateOnly? CreatedDateFrom     { get; set; }
    public DateOnly? CreatedDateTo       { get; set; }
    public DateOnly? UpdatedDateFrom     { get; set; }
    public DateOnly? UpdatedDateTo       { get; set; }
}
