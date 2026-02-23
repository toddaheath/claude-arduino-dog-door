using System.Security.Claims;
using Asp.Versioning;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogDoor.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly INotificationPreferencesService _notifPrefsService;

    public UsersController(IUserService userService, INotificationPreferencesService notifPrefsService)
    {
        _userService = userService;
        _notifPrefsService = notifPrefsService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var profile = await _userService.GetProfileAsync(CurrentUserId);
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateUserProfileDto dto)
    {
        var profile = await _userService.UpdateProfileAsync(CurrentUserId, dto);
        return Ok(profile);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var success = await _userService.ChangePasswordAsync(CurrentUserId, dto);
        if (!success) return BadRequest("Current password is incorrect");
        return NoContent();
    }

    [HttpGet("me/guests")]
    public async Task<ActionResult<IEnumerable<GuestDto>>> GetGuests()
    {
        var guests = await _userService.GetGuestsAsync(CurrentUserId);
        return Ok(guests);
    }

    [HttpPost("me/guests/invite")]
    public async Task<IActionResult> InviteGuest([FromBody] InviteGuestDto dto)
    {
        await _userService.InviteGuestAsync(CurrentUserId, dto.Email);
        return NoContent();
    }

    [HttpDelete("me/guests/{guestId}")]
    public async Task<IActionResult> RemoveGuest(int guestId)
    {
        await _userService.RemoveGuestAsync(CurrentUserId, guestId);
        return NoContent();
    }

    [HttpGet("me/invitations")]
    public async Task<ActionResult<IEnumerable<InvitationDto>>> GetInvitations()
    {
        var invitations = await _userService.GetPendingInvitationsAsync(CurrentUserId);
        return Ok(invitations);
    }

    [HttpPost("me/invitations/accept/{token}")]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        try
        {
            await _userService.AcceptInvitationAsync(token, CurrentUserId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("me/notifications")]
    public async Task<ActionResult<NotificationPreferencesDto>> GetNotificationPreferences()
    {
        var prefs = await _notifPrefsService.GetAsync(CurrentUserId);
        return Ok(prefs);
    }

    [HttpPut("me/notifications")]
    public async Task<ActionResult<NotificationPreferencesDto>> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesDto dto)
    {
        var prefs = await _notifPrefsService.UpdateAsync(CurrentUserId, dto);
        return Ok(prefs);
    }
}
