using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface INoteRepository
{
    Task<IEnumerable<Note>> GetAllAsync(int? linkObjectTypeId = null, int? linkObjectId = null);
    Task<Note?> GetByIdAsync(int id);
    Task<bool> LinkTargetExistsAsync(int linkObjectTypeId, int linkObjectId);
    Task<Note> CreateAsync(Note note);
    Task<bool> UpdateAsync(Note note);
    Task<bool> DeleteAsync(int id);
}
