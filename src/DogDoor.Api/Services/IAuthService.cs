using DogDoor.Api.DTOs;
using DogDoor.Api.Models;

namespace DogDoor.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<AuthResponseDto?> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task ForgotUsernameAsync(string email);
    Task<AuthResponseDto?> ExternalLoginAsync(ExternalLoginProvider provider, string idToken);
    Task<bool> LinkExternalLoginAsync(int userId, ExternalLoginProvider provider, string providerUserId, string? providerEmail);
}
