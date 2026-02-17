namespace DogDoor.Api.DTOs;

public record UserProfileDto(
    int Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? MobilePhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    bool EmailVerified,
    DateTime CreatedAt
);

public record UpdateUserProfileDto(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? MobilePhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country
);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);

public record GuestDto(
    int UserId,
    string Email,
    string? FirstName,
    string? LastName,
    DateTime InvitedAt,
    DateTime? AcceptedAt
);

public record InviteGuestDto(string Email);

public record InvitationDto(
    int Id,
    string InviteeEmail,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsAccepted
);
