using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IEventImageRepository
{
    Task<bool> EventExistsAsync(int eventId);
    Task<IEnumerable<EventImage>> GetForEventAsync(int eventId);
    Task<EventImage?> GetFileAsync(int id);
    Task<EventImage> CreateAsync(EventImage image);
    Task<bool> DeleteAsync(int id);
}
