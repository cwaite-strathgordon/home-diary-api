namespace HomeDiary_api.Models;

public class User
{
    public int     UserId        { get; set; }
    public int?    ClientId      { get; set; }
    public string? ClientName    { get; set; }
    public string? InboundEmailAddress { get; set; }
    public string? FirstName     { get; set; }
    public string? LastName      { get; set; }
    public string? Email         { get; set; }
    public bool    Admin         { get; set; }
    public bool    Disabled      { get; set; }
    public string? MobileNumber  { get; set; }
    public string? OAuthProvider { get; set; }
    public string? OAuthId       { get; set; }
    public string? OAuthEmail    { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool OnboardingComplete => ClientId.HasValue;
}
