using DogDoor.Api.Models;

namespace DogDoor.Api.DTOs;

public record RegisterDto(
    string Email,
    string Password,
    string? FirstName,
    string? LastName
);

public record LoginDto(
    string Email,
    string Password
);

public record RefreshTokenRequestDto(string RefreshToken);

public record LogoutDto(string RefreshToken);

public record ForgotPasswordDto(string Email);

public record ResetPasswordDto(string Token, string NewPassword);

public record ForgotUsernameDto(string Email);

public record UserSummaryDto(
    int Id,
    string Email,
    string? FirstName,
    string? LastName
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserSummaryDto User
);

public record ExternalLoginCallbackDto(
    ExternalLoginProvider Provider,
    string IdToken,
    string? AccessToken
);

public record LinkExternalLoginDto(
    ExternalLoginProvider Provider,
    string ProviderUserId,
    string? ProviderEmail
);
