using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<IEnumerable<GuestDto>> GetGuestsAsync(int userId);
    Task InviteGuestAsync(int userId, string guestEmail);
    Task AcceptInvitationAsync(string token, int guestUserId);
    Task RemoveGuestAsync(int ownerId, int guestId);
    Task<IEnumerable<InvitationDto>> GetPendingInvitationsAsync(int userId);
    Task<int> ResolveEffectiveUserIdAsync(int currentUserId, int? asOwner);
}
