using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IOnboardingRepository
{
    Task<OnboardingSuggestions> GetSuggestionsAsync(OnboardingSuggestionRequest request);
    Task<User> CompleteAsync(int userId, CompleteOnboardingRequest request, CancellationToken cancellationToken);
}
