using HomeDiary_api.Models;
using HomeDiary_api.Repositories;
using HomeDiary_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HomeDiary_api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "HomeDiaryAdmin")]
public class UserController(
    IUserRepository repo,
    IClientInvitationRepository invitations,
    IInvitationEmailSender invitationEmail,
    ILogger<UserController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await repo.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await repo.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        var user = await repo.GetByIdAsync(id);
        if (user is null) return NotFound();

        var currentUserId = User.FindFirst("homediary_user_id")?.Value;
        if (currentUserId == id.ToString() && (!request.Admin || request.Disabled))
            return BadRequest("You cannot remove your own administrator access or disable your own account.");

        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();
        user.Email = request.Email?.Trim();
        user.MobileNumber = request.MobileNumber?.Trim();
        user.Admin = request.Admin;
        user.Disabled = request.Disabled;

        var updated = await repo.UpdateAsync(user);
        return updated ? NoContent() : NotFound();
    }

    [HttpGet("invitations")]
    public async Task<ActionResult<IEnumerable<ClientInvitation>>> GetInvitations() =>
        Ok(await invitations.GetAllAsync());

    [HttpPost("invitations")]
    public async Task<ActionResult<ClientInvitation>> Invite(
        InviteUserRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("homediary_user_id")?.Value, out var userId)) return Unauthorized();
        var existing = await repo.GetByEmailAsync(request.Email);
        if (existing is not null) return Conflict("That email address already belongs to this client.");
        var inviter = await repo.GetByIdAsync(userId);
        if (inviter is null) return Unauthorized();
        var invitation = await invitations.CreateAsync(request.Email, request.Admin, userId);
        try
        {
            var messageId = await invitationEmail.SendAsync(
                invitation,
                inviter.ClientName ?? "your HomeDiary workspace",
                DisplayName(inviter),
                cancellationToken);
            await invitations.MarkSentAsync(invitation.ClientInvitationId, messageId);
            invitation.SentAt = DateTimeOffset.UtcNow;
            invitation.SesMessageId = messageId;
            invitation.DeliveryError = null;
            return Ok(invitation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SES failed to send invitation {InvitationId}", invitation.ClientInvitationId);
            await invitations.MarkDeliveryFailedAsync(invitation.ClientInvitationId, ex.Message);
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "Invitation email could not be sent",
                Detail = "The invitation was saved but Amazon SES did not accept the message. Check the SES identity, sandbox status, AWS credentials and IAM permissions, then resend it."
            });
        }
    }

    [HttpDelete("invitations/{id:long}")]
    public async Task<IActionResult> RevokeInvitation(long id) =>
        await invitations.RevokeAsync(id) ? NoContent() : NotFound();

    private static string DisplayName(Models.User user) =>
        string.Join(' ', new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)))
            is { Length: > 0 } name ? name : user.Email ?? "A HomeDiary administrator";
}

public sealed class InviteUserRequest
{
    [Required, EmailAddress, StringLength(320)] public string Email { get; set; } = string.Empty;
    public bool Admin { get; set; }
}

public class UpdateUserRequest
{
    [StringLength(255)]
    public string? FirstName { get; set; }

    [StringLength(255)]
    public string? LastName { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? MobileNumber { get; set; }

    public bool Admin { get; set; }
    public bool Disabled { get; set; }
}
