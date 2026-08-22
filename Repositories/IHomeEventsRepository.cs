using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IHomeEventsRepository
{
    Task<IEnumerable<HomeEventDetail>> GetByFilterAsync(HomeEventFilter filter);
    Task<EventTaskSummary>             GetTaskSummaryAsync();
    Task<HomeEventDetail?>             GetByIdAsync(int id);
    Task<IEnumerable<HomeEventDetail>> GetByUserAsync(int userId);
    Task<HomeEvent>                    CreateAsync(HomeEvent homeEvent);
    Task<bool>                         UpdateAsync(HomeEvent homeEvent);
    Task<bool>                         DeleteAsync(int id);
    Task<CompleteEventResult?>         CompleteAsync(int id);
    Task<bool>                         ReopenAsync(int id);
}
