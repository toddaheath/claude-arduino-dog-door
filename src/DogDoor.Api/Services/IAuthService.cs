using DogDoor.Api.DTOs;

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
}
