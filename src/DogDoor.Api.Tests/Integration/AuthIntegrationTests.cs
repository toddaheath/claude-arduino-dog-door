using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DogDoor.Api.Tests.Integration;

/// <summary>
/// Separate factory that keeps real JWT auth so auth endpoints can be tested end-to-end.
/// </summary>
public class AuthWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "AuthIntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Provide a deterministic JWT secret so JwtService and the JWT middleware agree.
        builder.UseSetting("JWT:SecretKey", "test-jwt-secret-key-at-least-32-characters!!");

        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<DogDoorDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            services.AddDbContext<DogDoorDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Mock external services to prevent real network calls
            services.AddSingleton<IEmailService>(Mock.Of<IEmailService>());
            services.AddSingleton<ISmsService>(Mock.Of<ISmsService>());
            services.AddSingleton<INotificationService>(Mock.Of<INotificationService>());
        });
    }
}

public class AuthIntegrationTests : IClassFixture<AuthWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly AuthWebAppFactory _factory;

    public AuthIntegrationTests(AuthWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static RegisterDto UniqueRegister(string tag = "") =>
        new($"auth-test{tag}-{Guid.NewGuid():N}@example.com", "Password1!", null, null);

    private async Task<AuthResponseDto> RegisterAndGetTokens(RegisterDto dto)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidCredentials_ReturnsTokens()
    {
        var dto = UniqueRegister();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));
        Assert.Equal(dto.Email, auth.User.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var dto = UniqueRegister("-dup");
        await RegisterAndGetTokens(dto);

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var dto = UniqueRegister("-login");
        await RegisterAndGetTokens(dto);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginDto(dto.Email, dto.Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var dto = UniqueRegister("-badpw");
        await RegisterAndGetTokens(dto);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginDto(dto.Email, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var dto = UniqueRegister("-refresh");
        var initial = await RegisterAndGetTokens(dto);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequestDto(initial.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrEmpty(refreshed.AccessToken));
        Assert.NotEqual(initial.RefreshToken, refreshed.RefreshToken);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var dto = UniqueRegister("-logout");
        var auth = await RegisterAndGetTokens(dto);

        var logoutResponse = await _client.PostAsJsonAsync("/api/v1/auth/logout",
            new LogoutDto(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // Refreshing after logout should fail
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequestDto(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Create a fresh client with no Authorization header
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/v1/animals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
