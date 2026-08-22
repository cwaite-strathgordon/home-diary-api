using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IPropertySettingRepository
{
    Task<PropertySetting?> GetAsync();
    Task<PropertySetting> SaveAsync(PropertySetting setting);
}
