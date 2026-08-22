using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IGlobalSearchRepository
{
    Task<IEnumerable<GlobalSearchResult>> SearchAsync(string query, int limit = 30);
}
