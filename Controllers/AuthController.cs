using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IUserRepository repo) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<User>> GetProfile()
    {
        if (!TryGetHomeDiaryUserId(out var userId)) return Unauthorized();
        var user = await repo.GetByIdAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<User>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!TryGetHomeDiaryUserId(out var userId)) return Unauthorized();

        var user = await repo.GetByIdAsync(userId);
        if (user is null) return NotFound();

        var email = request.Email.Trim();
        var existingUser = await repo.GetByEmailAsync(email);
        if (existingUser is not null && existingUser.UserId != userId)
            return Conflict(new ProblemDetails
            {
                Title = "Email already in use",
                Detail = "That contact email is already assigned to another HomeDiary user."
            });

        user.FirstName = Clean(request.FirstName);
        user.LastName = Clean(request.LastName);
        user.Email = email;
        user.MobileNumber = Clean(request.MobileNumber);

        if (!await repo.UpdateAsync(user)) return NotFound();
        return Ok(await repo.GetByIdAsync(userId));
    }

    /// <summary>
    /// Finds the HomeDiary user linked to a given OAuth identity.
    /// Call this after a successful OAuth token exchange.
    /// </summary>
    [HttpGet("user-by-oauth")]
    public async Task<ActionResult<User>> GetByOAuth([FromQuery] string provider, [FromQuery] string oauthId)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(oauthId))
            return BadRequest("provider and oauthId are required.");

        var authenticatedSubject = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.Equals(provider, "auth0", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(oauthId, authenticatedSubject, StringComparison.Ordinal))
            return Forbid();
        var user = await repo.GetByOAuthAsync(provider, oauthId);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Finds a HomeDiary user by email address.
    /// Useful for linking an existing account before writing OAuth credentials.
    /// </summary>
    [HttpGet("user-by-email")]
    public async Task<ActionResult<User>> GetByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("email is required.");

        var user = await repo.GetByEmailAsync(email);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Links an OAuth identity to an existing HomeDiary user.
    /// Use when the user is already logged in and wants to connect their OAuth account.
    /// </summary>
    [HttpPost("link-oauth")]
    public async Task<IActionResult> LinkOAuth([FromBody] LinkOAuthRequest request)
    {
        var linked = await repo.LinkOAuthAsync(
            request.UserId, request.Provider, request.OAuthId, request.OAuthEmail);

        return linked ? NoContent() : NotFound();
    }

    /// <summary>
    /// Creates or updates the HomeDiary user for the authenticated Auth0 subject.
    /// The OAuth identity comes from the validated access token, not the request body.
    /// </summary>
    [HttpPost("upsert-oauth-user")]
    public async Task<ActionResult<User>> UpsertFromOAuth([FromBody] OAuthUserRequest request)
    {
        var oauthId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(oauthId))
            return Unauthorized("The authenticated token does not contain a subject claim.");

        // Auth0 access tokens do not always contain profile claims, so use claims
        // when available and fall back to the profile supplied by the Auth0 client.
        var tokenEmail = User.FindFirst("email")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("https://homediary.app/email")?.Value;
        var email = tokenEmail ?? request.Email;
        var emailVerified = tokenEmail is not null &&
            bool.TryParse(User.FindFirst("email_verified")?.Value
                ?? User.FindFirst("https://homediary.app/email_verified")?.Value,
                out var verified) && verified;
        var firstName = User.FindFirst("given_name")?.Value
            ?? User.FindFirst(ClaimTypes.GivenName)?.Value
            ?? request.FirstName;
        var lastName = User.FindFirst("family_name")?.Value
            ?? User.FindFirst(ClaimTypes.Surname)?.Value
            ?? request.LastName;

        var user = await repo.UpsertFromOAuthAsync(
            "auth0", oauthId, email, firstName, lastName, emailVerified, request.InvitationToken);

        if (user.Disabled)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ProblemDetails { Title = "Account disabled", Detail = "Your HomeDiary account has been disabled." });

        if (request.InvitationToken.HasValue && user.ClientId is null)
            return Conflict(new ProblemDetails
            {
                Title = "Invitation could not be accepted",
                Detail = emailVerified
                    ? "The invitation is invalid, expired, already used, or belongs to a different email address."
                    : "Auth0 did not provide a verified email claim. Sign in with the invited verified email after configuring the HomeDiary Auth0 claims."
            });

        return Ok(user);
    }

    private bool TryGetHomeDiaryUserId(out int userId) =>
        int.TryParse(User.FindFirst("homediary_user_id")?.Value, out userId);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public record LinkOAuthRequest(int UserId, string Provider, string OAuthId, string? OAuthEmail);
public record OAuthUserRequest(string? Email, string? FirstName, string? LastName, Guid? InvitationToken);

public class UpdateProfileRequest
{
    [StringLength(255)]
    public string? FirstName { get; set; }

    [StringLength(255)]
    public string? LastName { get; set; }

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? MobileNumber { get; set; }
}
