using DogDoor.Api.Models;

namespace DogDoor.Api.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int? GetUserIdFromToken(string token);
}
