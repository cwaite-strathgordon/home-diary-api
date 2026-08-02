using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IEventStatusRepository
{
    Task<IEnumerable<EventStatus>> GetAllAsync();
    Task<EventStatus?>             GetByIdAsync(int id);
    Task<EventStatus>              CreateAsync(EventStatus status);
    Task<bool>                     UpdateAsync(EventStatus status);
    Task<bool>                     DeleteAsync(int id);
}
