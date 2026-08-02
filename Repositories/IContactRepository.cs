using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IContactRepository
{
    Task<IEnumerable<Contact>> GetAllAsync();
    Task<Contact?>             GetByIdAsync(int id);
    Task<Contact>              CreateAsync(Contact contact);
    Task<bool>                 UpdateAsync(Contact contact);
    Task<bool>                 DeleteAsync(int id);
}
