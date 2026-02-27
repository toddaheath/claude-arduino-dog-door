using System.Security.Cryptography;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class UserService : IUserService
{
    private readonly DogDoorDbContext _db;
    private readonly IEmailService _emailService;

    public UserService(DogDoorDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        return MapToProfileDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (dto.FirstName != null) user.FirstName = dto.FirstName;
        if (dto.LastName != null) user.LastName = dto.LastName;
        if (dto.Phone != null) user.Phone = dto.Phone;
        if (dto.MobilePhone != null) user.MobilePhone = dto.MobilePhone;
        if (dto.AddressLine1 != null) user.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 != null) user.AddressLine2 = dto.AddressLine2;
        if (dto.City != null) user.City = dto.City;
        if (dto.State != null) user.State = dto.State;
        if (dto.PostalCode != null) user.PostalCode = dto.PostalCode;
        if (dto.Country != null) user.Country = dto.Country;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToProfileDto(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        // SSO-only users have no password hash — cannot change password
        if (user.PasswordHash == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 12);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GuestDto>> GetGuestsAsync(int userId)
    {
        var guests = await _db.UserGuests
            .Include(g => g.Guest)
            .Where(g => g.OwnerId == userId)
            .ToListAsync();

        return guests.Select(g => new GuestDto(
            g.GuestId,
            g.Guest.Email,
            g.Guest.FirstName,
            g.Guest.LastName,
            g.InvitedAt,
            g.AcceptedAt
        ));
    }

    public async Task InviteGuestAsync(int userId, string guestEmail)
    {
        var owner = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Owner not found");

        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var hashedToken = HashTokenSha256(rawToken);

        var invitation = new Invitation
        {
            InvitedById = userId,
            InviteeEmail = guestEmail,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();

        var ownerName = string.IsNullOrWhiteSpace(owner.FirstName)
            ? owner.Email
            : $"{owner.FirstName} {owner.LastName}".Trim();

        await _emailService.SendGuestInvitationAsync(guestEmail, ownerName, rawToken);
    }

    public async Task AcceptInvitationAsync(string token, int guestUserId)
    {
        var hashedToken = HashTokenSha256(token);
        var invitation = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Token == hashedToken && i.AcceptedAt == null && i.ExpiresAt > DateTime.UtcNow)
            ?? throw new InvalidOperationException("Invalid or expired invitation token");

        // Create guest relationship
        var existing = await _db.UserGuests
            .FindAsync(invitation.InvitedById, guestUserId);

        if (existing == null)
        {
            _db.UserGuests.Add(new UserGuest
            {
                OwnerId = invitation.InvitedById,
                GuestId = guestUserId,
                InvitedAt = invitation.CreatedAt,
                AcceptedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.AcceptedAt = DateTime.UtcNow;
        }

        invitation.AcceptedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveGuestAsync(int ownerId, int guestId)
    {
        var guest = await _db.UserGuests.FindAsync(ownerId, guestId);
        if (guest != null)
        {
            _db.UserGuests.Remove(guest);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<InvitationDto>> GetPendingInvitationsAsync(int userId)
    {
        var invitations = await _db.Invitations
            .Where(i => i.InvitedById == userId && i.AcceptedAt == null && i.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        return invitations.Select(i => new InvitationDto(
            i.Id,
            i.InviteeEmail,
            i.CreatedAt,
            i.ExpiresAt,
            i.AcceptedAt.HasValue
        ));
    }

    public async Task<int> ResolveEffectiveUserIdAsync(int currentUserId, int? asOwner)
    {
        if (!asOwner.HasValue || asOwner.Value == currentUserId)
            return currentUserId;

        var isGuest = await _db.UserGuests
            .AnyAsync(g => g.OwnerId == asOwner.Value && g.GuestId == currentUserId && g.AcceptedAt != null);

        if (!isGuest)
            throw new UnauthorizedAccessException("Not a guest of the specified owner");

        return asOwner.Value;
    }

    private static string HashTokenSha256(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static UserProfileDto MapToProfileDto(Models.User user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Phone,
        user.MobilePhone,
        user.AddressLine1,
        user.AddressLine2,
        user.City,
        user.State,
        user.PostalCode,
        user.Country,
        user.EmailVerified,
        user.CreatedAt
    );
}
