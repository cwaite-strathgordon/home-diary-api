using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Configuration;

public sealed class InvitationEmailOptions
{
    public const string SectionName = "InvitationEmail";

    [Required] public string Region { get; set; } = "eu-west-2";
    [Required, EmailAddress] public string FromAddress { get; set; } = "no-reply@homediary.app";
    [Required] public string FromName { get; set; } = "HomeDiary";
    [EmailAddress] public string? ReplyToAddress { get; set; }
    [Required, Url] public string ApplicationBaseUrl { get; set; } = "http://localhost:4200";
}
