namespace HomeDiary_api.Models;

public class HomeEventFilter
{
    public string?   TitleContains       { get; set; }
    public string?   DescriptionContains { get; set; }
    public int?      EventTypeId         { get; set; }
    public int?      AreaId              { get; set; }
    public int?      EventStatusId       { get; set; }
    public int?      CreatedById         { get; set; }
    public DateOnly? EventDateFrom       { get; set; }
    public DateOnly? EventDateTo         { get; set; }
    public DateOnly? CreatedDateFrom     { get; set; }
    public DateOnly? CreatedDateTo       { get; set; }
    public DateOnly? UpdatedDateFrom     { get; set; }
    public DateOnly? UpdatedDateTo       { get; set; }
}
