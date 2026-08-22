using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IApplicationParameterRepository
{
    Task<AiSettings> GetAiSettingsAsync();
    Task<AiSettings> UpdateAiSettingsAsync(UpdateAiSettingsRequest request, int updatedById);
    Task<ApplicationSettings> GetApplicationSettingsAsync();
    Task<ApplicationSettings> UpdateApplicationSettingsAsync(UpdateApplicationSettingsRequest request, int updatedById);
}
