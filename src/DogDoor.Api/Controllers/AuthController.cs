using Asp.Versioning;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DogDoor.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }

    private static AuthResponseBodyDto ToBodyDto(AuthResponseDto auth) =>
        new(auth.AccessToken, auth.ExpiresAt, auth.User);

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseBodyDto>> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(ToBodyDto(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseBodyDto>> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result == null) return Unauthorized("Invalid credentials");
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ToBodyDto(result));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseBodyDto>> Refresh()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized("No refresh token");
        }

        var result = await _authService.RefreshAsync(refreshToken);
        if (result == null)
        {
            ClearRefreshTokenCookie();
            return Unauthorized("Invalid or expired refresh token");
        }

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ToBodyDto(result));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken);
        }
        ClearRefreshTokenCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto.Email);
        return NoContent();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("forgot-username")]
    public async Task<IActionResult> ForgotUsername([FromBody] ForgotUsernameDto dto)
    {
        await _authService.ForgotUsernameAsync(dto.Email);
        return NoContent();
    }

    [HttpPost("external/{provider}")]
    public async Task<ActionResult<AuthResponseBodyDto>> ExternalLogin(
        ExternalLoginProvider provider,
        [FromBody] ExternalLoginCallbackDto dto)
    {
        _ = await _authService.ExternalLoginAsync(provider, dto.IdToken);
        return StatusCode(501, $"SSO provider '{provider}' not yet configured");
    }
}
