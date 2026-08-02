using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IUserRepository repo) : ControllerBase
{
    /// <summary>
    /// Finds the HomeDiary user linked to a given OAuth identity.
    /// Call this after a successful OAuth token exchange.
    /// </summary>
    [HttpGet("user-by-oauth")]
    public async Task<ActionResult<User>> GetByOAuth([FromQuery] string provider, [FromQuery] string oauthId)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(oauthId))
            return BadRequest("provider and oauthId are required.");

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
    /// Creates or updates a HomeDiary user from an OAuth callback profile.
    /// Match logic: find by (provider + oauthId) first, then fall back to email.
    /// Returns the user record so the caller can issue an app session token.
    /// </summary>
    [HttpPost("upsert-oauth-user")]
    public async Task<ActionResult<User>> UpsertFromOAuth([FromBody] OAuthUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider) ||
            string.IsNullOrWhiteSpace(request.OAuthId)  ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("provider, oauthId, and email are required.");
        }

        var user = await repo.UpsertFromOAuthAsync(
            request.Provider, request.OAuthId, request.Email,
            request.FirstName, request.LastName);

        return Ok(user);
    }
}

public record LinkOAuthRequest(int UserId, string Provider, string OAuthId, string? OAuthEmail);
public record OAuthUserRequest(string Provider, string OAuthId, string Email, string? FirstName, string? LastName);
