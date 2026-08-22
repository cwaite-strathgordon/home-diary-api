using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IClientInvitationRepository
{
    Task<IEnumerable<ClientInvitation>> GetAllAsync();
    Task<ClientInvitation> CreateAsync(string email, bool admin, int invitedById);
    Task MarkSentAsync(long id, string sesMessageId);
    Task MarkDeliveryFailedAsync(long id, string error);
    Task<bool> RevokeAsync(long id);
}
