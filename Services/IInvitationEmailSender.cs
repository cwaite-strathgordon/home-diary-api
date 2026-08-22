using HomeDiary_api.Models;

namespace HomeDiary_api.Services;

public interface IInvitationEmailSender
{
    Task<string> SendAsync(
        ClientInvitation invitation,
        string clientName,
        string invitedByName,
        CancellationToken cancellationToken);
}
