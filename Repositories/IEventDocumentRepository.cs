using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IEventDocumentRepository
{
    Task<bool> EventExistsAsync(int eventId);
    Task<IEnumerable<EventDocument>> GetForEventAsync(int eventId);
    Task<IEnumerable<EventDocument>> GetAllAsync();
    Task<int> GetCountAsync();
    Task<IEnumerable<EventDocument>> SearchAsync(string query, int? eventId);
    Task<EventDocument?> GetFileAsync(int id);
    Task<EventDocument> CreateAsync(EventDocument document);
    Task<bool> DeleteAsync(int id);
}
