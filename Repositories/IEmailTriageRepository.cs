using HomeDiary_api.Models;

namespace HomeDiary_api.Repositories;

public interface IEmailTriageRepository
{
    Task<EmailTriagePage> GetAsync(string? status, int limit, int offset);
    Task<EmailIntakeDetail?> GetByIdAsync(long id);
    Task<bool> ReviewIntakeAsync(long id, EmailIntakeReviewRequest review, int reviewerId);
    Task<EmailSuggestionReviewResult?> ReviewSuggestionAsync(
        long intakeId,
        long suggestionId,
        EmailSuggestionReviewRequest review,
        int reviewerId);
}
