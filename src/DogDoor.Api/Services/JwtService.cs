using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DogDoor.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace DogDoor.Api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(User user)
    {
        var secretKey = _config["JWT:SecretKey"]
            ?? throw new InvalidOperationException("JWT:SecretKey not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
        };

        var token = new JwtSecurityToken(
            issuer: _config["JWT:Issuer"],
            audience: _config["JWT:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public int? GetUserIdFromToken(string token)
    {
        try
        {
            var secretKeyValue = _config["JWT:SecretKey"];
            if (secretKeyValue == null) return null;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKeyValue));
            var handler = new JwtSecurityTokenHandler();
            var validations = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false // Allow expired tokens for refresh flow
            };

            var principal = handler.ValidateToken(token, validations, out _);
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return value != null ? int.Parse(value) : null;
        }
        catch
        {
            return null;
        }
    }
}
