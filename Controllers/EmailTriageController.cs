using System.Security.Claims;
using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Authorize(Policy = "HomeDiaryAdmin")]
[Route("api/email-triage")]
public class EmailTriageController(IEmailTriageRepository repo) : ControllerBase
{
    private static readonly HashSet<string> IntakeStatuses =
        ["pending", "processing", "needs_review", "quarantined", "rejected", "completed", "failed"];

    private static readonly HashSet<string> ReviewableIntakeStatuses =
        ["pending", "needs_review", "quarantined", "rejected"];

    private static readonly HashSet<string> ReviewableSuggestionStatuses =
        ["pending", "approved", "rejected"];

    [HttpGet]
    public async Task<ActionResult<EmailTriagePage>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        status = status?.Trim().ToLowerInvariant();
        if (status is not null && !IntakeStatuses.Contains(status))
            return BadRequest("Invalid email intake status.");
        if (limit is < 1 or > 200) return BadRequest("Limit must be between 1 and 200.");
        if (offset < 0) return BadRequest("Offset cannot be negative.");

        return Ok(await repo.GetAsync(status, limit, offset));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<EmailIntakeDetail>> GetById(long id)
    {
        var email = await repo.GetByIdAsync(id);
        return email is null ? NotFound() : Ok(email);
    }

    [HttpPatch("{id:long}/review")]
    public async Task<IActionResult> ReviewIntake(long id, EmailIntakeReviewRequest review)
    {
        review.Status = review.Status.Trim().ToLowerInvariant();
        review.TriageSummary = review.TriageSummary?.Trim();
        review.TriageReason = review.TriageReason?.Trim();
        if (!ReviewableIntakeStatuses.Contains(review.Status))
            return BadRequest("Review status must be pending, needs_review, quarantined or rejected.");

        var reviewerId = GetCurrentUserId();
        return await repo.ReviewIntakeAsync(id, review, reviewerId) ? NoContent() : NotFound();
    }

    [HttpPatch("{intakeId:long}/suggestions/{suggestionId:long}/review")]
    public async Task<ActionResult<EmailSuggestionReviewResult>> ReviewSuggestion(
        long intakeId,
        long suggestionId,
        EmailSuggestionReviewRequest review)
    {
        review.Status = review.Status.Trim().ToLowerInvariant();
        review.ReviewNotes = review.ReviewNotes?.Trim();
        if (!ReviewableSuggestionStatuses.Contains(review.Status))
            return BadRequest("Suggestion status must be pending, approved or rejected.");

        var reviewerId = GetCurrentUserId();
        try
        {
            var result = await repo.ReviewSuggestionAsync(
                intakeId, suggestionId, review, reviewerId);
            return result is null ? NotFound() : Ok(result);
        }
        catch (EmailSuggestionApplicationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "The task suggestion could not be applied",
                Detail = ex.Message
            });
        }
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue("homediary_user_id");
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated HomeDiary user ID is missing.");
    }
}
