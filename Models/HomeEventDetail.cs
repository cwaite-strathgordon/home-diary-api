namespace HomeDiary_api.Models;

public class HomeEventDetail : HomeEvent
{
    public string? EventTypeTitle     { get; set; }
    public string? AreaTitle          { get; set; }
    public string? EventStatusTitle   { get; set; }
    public string? CreatedByFirstName { get; set; }
    public string? CreatedByLastName  { get; set; }
}
