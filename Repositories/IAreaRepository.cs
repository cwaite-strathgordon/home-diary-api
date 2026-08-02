using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IAreaRepository
{
    Task<IEnumerable<Area>> GetAllAsync();
    Task<Area?>             GetByIdAsync(int id);
    Task<Area>              CreateAsync(Area area);
    Task<bool>              UpdateAsync(Area area);
    Task<bool>              DeleteAsync(int id);
}
