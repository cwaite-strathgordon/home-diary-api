using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetAllAsync(bool includeArchived = false);
    Task<Project?> GetByIdAsync(int id);
    Task<Project> CreateAsync(Project project);
    Task<bool> UpdateAsync(Project project);
    Task<bool> ArchiveAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<bool> DeleteAsync(int id);
}
