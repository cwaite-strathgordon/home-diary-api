using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?>             GetByIdAsync(int id);
    Task<User?>             GetByEmailAsync(string email);
    Task<User?>             GetByOAuthAsync(string provider, string oauthId);
    Task<User>              CreateAsync(User user);
    Task<bool>              UpdateAsync(User user);
    Task<bool>              DeleteAsync(int id);
    Task<bool>              LinkOAuthAsync(int userId, string provider, string oauthId, string? oauthEmail);
    Task<User>              UpsertFromOAuthAsync(string provider, string oauthId, string email, string? firstName, string? lastName);
}
