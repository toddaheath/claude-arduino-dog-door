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
        // Disable rate limiting in tests
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");

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
    private readonly CookieContainerHandler _cookieHandler;

    /// <summary>
    /// DelegatingHandler that manually tracks Set-Cookie / Cookie headers,
    /// since TestServer's in-memory transport bypasses HttpClientHandler.CookieContainer.
    /// </summary>
    private class CookieContainerHandler : DelegatingHandler
    {
        private readonly Dictionary<string, string> _cookies = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Attach stored cookies to outgoing request
            if (_cookies.Count > 0)
            {
                var cookieHeader = string.Join("; ", _cookies.Select(kv => $"{kv.Key}={kv.Value}"));
                request.Headers.Add("Cookie", cookieHeader);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Parse Set-Cookie headers from response
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var header in setCookieHeaders)
                {
                    var parts = header.Split(';', 2);
                    var nameValue = parts[0].Trim();
                    var eqIndex = nameValue.IndexOf('=');
                    if (eqIndex <= 0) continue;

                    var name = nameValue[..eqIndex];
                    var value = nameValue[(eqIndex + 1)..];

                    // Check for expiry in the past (cookie deletion)
                    var lowerHeader = header.ToLowerInvariant();
                    if (lowerHeader.Contains("expires=") && lowerHeader.Contains("thu, 01 jan 1970"))
                    {
                        _cookies.Remove(name);
                    }
                    else if (string.IsNullOrEmpty(value))
                    {
                        _cookies.Remove(name);
                    }
                    else
                    {
                        _cookies[name] = value;
                    }
                }
            }

            return response;
        }

        public bool HasCookie(string name) => _cookies.ContainsKey(name);
    }

    private record AuthResponseBody(string AccessToken, DateTime ExpiresAt, UserSummaryDto User);

    public AuthIntegrationTests(AuthWebAppFactory factory)
    {
        _factory = factory;
        _cookieHandler = new CookieContainerHandler();
        _client = factory.CreateDefaultClient(_cookieHandler);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static RegisterDto UniqueRegister(string tag = "") =>
        new($"auth-test{tag}-{Guid.NewGuid():N}@example.com", "Password1!", null, null);

    private async Task<AuthResponseBody> RegisterAndGetTokens(RegisterDto dto)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
        return result!;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidCredentials_ReturnsTokens()
    {
        var dto = UniqueRegister();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.Equal(dto.Email, auth.User.Email);
        // Refresh token should be in cookie, not in body
        Assert.True(_cookieHandler.HasCookie("refresh_token"));
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

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
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
    public async Task Refresh_WithValidCookie_ReturnsNewTokens()
    {
        var dto = UniqueRegister("-refresh");
        await RegisterAndGetTokens(dto);

        // Cookie is automatically sent by CookieContainerHandler
        var response = await _client.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponseBody>();
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrEmpty(refreshed.AccessToken));
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var dto = UniqueRegister("-logout");
        await RegisterAndGetTokens(dto);

        var logoutResponse = await _client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // Refreshing after logout should fail
        var refreshResponse = await _client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Create a fresh client with no Authorization header and no cookies
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/v1/animals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAccessToken_Succeeds()
    {
        var dto = UniqueRegister("-protected");
        var auth = await RegisterAndGetTokens(dto);

        // Use access token from response body in Authorization header
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.GetAsync("/api/v1/animals");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Clean up default header for other tests
        _client.DefaultRequestHeaders.Authorization = null;
    }
}
