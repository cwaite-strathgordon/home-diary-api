using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IEventContactLinkRepository
{
    Task<IEnumerable<EventContactLink>> GetByEventIdAsync(int eventId);
    Task<IEnumerable<EventContactLink>> GetByContactIdAsync(int contactId);
    Task<bool>                          CreateAsync(EventContactLink link);
    Task<bool>                          DeleteAsync(int contactId, int eventId);
}
