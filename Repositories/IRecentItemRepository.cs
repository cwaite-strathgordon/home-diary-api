using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IRecentItemRepository
{
    Task<IEnumerable<RecentItem>> GetAsync(int userId);
    Task<RecentItem?> RecordAsync(string itemType, int itemId, int userId);
}
