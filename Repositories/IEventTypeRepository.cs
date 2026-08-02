using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IEventTypeRepository
{
    Task<IEnumerable<EventType>> GetAllAsync();
    Task<EventType?>             GetByIdAsync(int id);
    Task<EventType>              CreateAsync(EventType type);
    Task<bool>                   UpdateAsync(EventType type);
    Task<bool>                   DeleteAsync(int id);
}
