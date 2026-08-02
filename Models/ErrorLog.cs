namespace HomeDiary_api.Models;

public class ErrorLog
{
    public int      ErrorLogId    { get; set; }
    public string?  ErrorMessage  { get; set; }
    public string?  StackTrace    { get; set; }
    public string?  Source        { get; set; }
    public DateTime CreatedDate   { get; set; }
}
