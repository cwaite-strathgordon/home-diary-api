using System.Net;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using HomeDiary_api.Configuration;
using HomeDiary_api.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace HomeDiary_api.Services;

public sealed class SesInvitationEmailSender(
    IAmazonSimpleEmailServiceV2 ses,
    IOptions<InvitationEmailOptions> options,
    ILogger<SesInvitationEmailSender> logger) : IInvitationEmailSender
{
    private readonly InvitationEmailOptions _options = options.Value;

    public async Task<string> SendAsync(
        ClientInvitation invitation,
        string clientName,
        string invitedByName,
        CancellationToken cancellationToken)
    {
        var invitationUrl = $"{_options.ApplicationBaseUrl.TrimEnd('/')}/login?invitation={invitation.InvitationToken:D}";
        var safeClient = WebUtility.HtmlEncode(clientName);
        var safeInviter = WebUtility.HtmlEncode(invitedByName);
        var safeUrl = WebUtility.HtmlEncode(invitationUrl);
        var role = invitation.Admin ? "administrator" : "user";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(invitation.Email));
        if (!string.IsNullOrWhiteSpace(_options.ReplyToAddress))
            message.ReplyTo.Add(MailboxAddress.Parse(_options.ReplyToAddress));
        message.Subject = $"You’re invited to {clientName} on HomeDiary";

        var builder = new BodyBuilder();
        var logoPath = Path.Combine(AppContext.BaseDirectory, "EmailAssets", "title-bar-app-name.png");
        string logoMarkup;
        if (File.Exists(logoPath))
        {
            var logo = builder.LinkedResources.Add(logoPath);
            logo.ContentId = MimeUtils.GenerateMessageId();
            logo.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            logoMarkup = $"<img src=\"cid:{logo.ContentId}\" width=\"220\" alt=\"HomeDiary\" style=\"display:block;width:220px;max-width:100%;height:auto;border:0\">";
        }
        else
        {
            logger.LogWarning("Invitation email logo was not found at {LogoPath}", logoPath);
            logoMarkup = "<span style=\"font-size:28px;font-weight:700;color:#2a7a7a\">HomeDiary</span>";
        }

        builder.HtmlBody = $$"""
        <!doctype html>
        <html lang="en">
        <body style="margin:0;padding:0;background:#f5f1e8;font-family:Arial,Helvetica,sans-serif;color:#292621">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f5f1e8;padding:32px 12px">
            <tr><td align="center">
              <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background:#ffffff;border:1px solid #d9d0c2;border-radius:14px;overflow:hidden">
                <tr><td style="background:#efe5d2;padding:24px 34px">{{logoMarkup}}</td></tr>
                <tr><td style="padding:38px 34px 16px">
                  <p style="margin:0 0 10px;color:#2a7a7a;font-size:12px;font-weight:700;letter-spacing:1.2px;text-transform:uppercase">Your home, organised</p>
                  <h1 style="margin:0 0 18px;font-size:28px;line-height:1.2;color:#27231e">You’ve been invited to HomeDiary</h1>
                  <p style="margin:0 0 14px;font-size:16px;line-height:1.6">{{safeInviter}} has invited you to join <strong>{{safeClient}}</strong> as {{(invitation.Admin ? "an administrator" : "a user")}}.</p>
                  <p style="margin:0 0 26px;font-size:15px;line-height:1.6;color:#625d55">Use HomeDiary to manage property tasks, maintenance schedules, projects, documents and contacts together.</p>
                  <table role="presentation" cellspacing="0" cellpadding="0"><tr><td style="background:#2a7a7a;border-radius:24px"><a href="{{safeUrl}}" style="display:inline-block;padding:13px 25px;color:#ffffff;text-decoration:none;font-size:15px;font-weight:700">Accept invitation</a></td></tr></table>
                  <p style="margin:24px 0 0;font-size:12px;line-height:1.6;color:#777168">This invitation expires {{invitation.ExpiresAt:dddd, d MMMM yyyy 'at' HH:mm 'UTC'}} and must be accepted using <strong>{{WebUtility.HtmlEncode(invitation.Email)}}</strong>.</p>
                </td></tr>
                <tr><td style="padding:18px 34px 30px"><div style="border-top:1px solid #e4ddd2;padding-top:18px;font-size:11px;line-height:1.6;color:#847d73">If the button does not work, copy this address into your browser:<br><a href="{{safeUrl}}" style="color:#2a7a7a;word-break:break-all">{{safeUrl}}</a></div></td></tr>
              </table>
              <p style="margin:16px 0 0;color:#8a8379;font-size:11px">Sent securely by HomeDiary</p>
            </td></tr>
          </table>
        </body>
        </html>
        """;
        builder.TextBody = $"""
        You’ve been invited to HomeDiary

        {invitedByName} has invited you to join {clientName} as {role}.

        Accept the invitation:
        {invitationUrl}

        This invitation expires {invitation.ExpiresAt:dddd, d MMMM yyyy 'at' HH:mm 'UTC'} and must be accepted using {invitation.Email}.
        """;
        message.Body = builder.ToMessageBody();

        await using var raw = new MemoryStream();
        await message.WriteToAsync(raw, cancellationToken);
        raw.Position = 0;
        var response = await ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = $"{_options.FromName} <{_options.FromAddress}>",
            Destination = new Destination { ToAddresses = [invitation.Email] },
            Content = new EmailContent { Raw = new RawMessage { Data = raw } }
        }, cancellationToken);
        logger.LogInformation(
            "Sent HomeDiary invitation {InvitationId} to {Recipient}; SES message {MessageId}",
            invitation.ClientInvitationId, invitation.Email, response.MessageId);
        return response.MessageId;
    }
}
