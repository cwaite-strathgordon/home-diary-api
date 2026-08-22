using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IEventPriorityRepository
{
    Task<IEnumerable<EventPriority>> GetAllAsync();
    Task<EventPriority?> GetByIdAsync(int id);
}
