using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class AuthService : IAuthService
{
    private readonly DogDoorDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;

    public AuthService(DogDoorDbContext db, IJwtService jwtService, IEmailService emailService)
    {
        _db = db;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (!IsValidEmail(dto.Email))
            throw new ArgumentException("Invalid email format");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already registered");

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12);

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow,
            EmailVerified = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var doorConfig = new DoorConfiguration
        {
            UserId = user.Id,
            IsEnabled = true,
            AutoCloseEnabled = true,
            AutoCloseDelaySeconds = 10,
            MinConfidenceThreshold = 0.7,
            UpdatedAt = DateTime.UtcNow
        };
        _db.DoorConfigurations.Add(doorConfig);
        await _db.SaveChangesAsync();

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            return null;

        token.IsRevoked = true;
        await _db.SaveChangesAsync();

        return await CreateAuthResponseAsync(token.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token != null)
        {
            token.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return; // Silent: don't reveal if email exists

        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var hashedToken = BCrypt.Net.BCrypt.HashPassword(rawToken, 12);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        _db.PasswordResetTokens.Add(resetToken);
        await _db.SaveChangesAsync();

        await _emailService.SendPasswordResetAsync(email, rawToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        // Find unexpired, unused reset tokens and check BCrypt hash
        var tokens = await _db.PasswordResetTokens
            .Include(t => t.User)
            .Where(t => !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var matchingToken = tokens.FirstOrDefault(t => BCrypt.Net.BCrypt.Verify(dto.Token, t.Token));
        if (matchingToken == null)
            throw new InvalidOperationException("Invalid or expired reset token");

        matchingToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 12);
        matchingToken.User.UpdatedAt = DateTime.UtcNow;
        matchingToken.IsUsed = true;
        await _db.SaveChangesAsync();
    }

    public async Task ForgotUsernameAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return; // Silent

        await _emailService.SendUsernameLookupAsync(email, email);
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();

        var tokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(tokenEntity);
        await _db.SaveChangesAsync();

        return new AuthResponseDto(
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(15),
            new UserSummaryDto(user.Id, user.Email, user.FirstName, user.LastName)
        );
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
