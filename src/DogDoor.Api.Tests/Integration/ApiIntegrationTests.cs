using System.Net;
using System.Net.Http.Json;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DogDoor.Api.Tests.Integration;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "IntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations to avoid multiple provider conflict
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<DogDoorDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Use in-memory database for tests
            services.AddDbContext<DogDoorDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace JWT authentication with test auth handler
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, null);
        });
    }
}

public class ApiIntegrationTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebAppFactory _factory;
    private const int TestUserId = 1;

    public ApiIntegrationTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, TestUserId.ToString());

        // Seed test user and door configuration once
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DogDoorDbContext>();
        if (!db.Users.Any(u => u.Id == TestUserId))
        {
            db.Users.Add(new User
            {
                Id = TestUserId,
                Email = "testuser1@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123", 12),
                CreatedAt = DateTime.UtcNow
            });
            db.DoorConfigurations.Add(new DoorConfiguration
            {
                UserId = TestUserId,
                IsEnabled = true,
                AutoCloseEnabled = true,
                AutoCloseDelaySeconds = 10,
                MinConfidenceThreshold = 0.7,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task GetAnimals_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/v1/animals");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateAndGetAnimal_RoundTrip()
    {
        var createDto = new CreateAnimalDto("IntBuddy", "Golden Retriever", true);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/animals", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<AnimalDto>();
        Assert.NotNull(created);
        Assert.Equal("IntBuddy", created.Name);

        var getResponse = await _client.GetAsync($"/api/v1/animals/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<AnimalDto>();
        Assert.NotNull(fetched);
        Assert.Equal("IntBuddy", fetched.Name);
        Assert.Equal("Golden Retriever", fetched.Breed);
    }

    [Fact]
    public async Task UpdateAnimal_ReturnsUpdated()
    {
        var createDto = new CreateAnimalDto("IntOldie", "Lab", true);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/animals", createDto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AnimalDto>();

        var updateDto = new UpdateAnimalDto("IntNewName", null, false);
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/animals/{created!.Id}", updateDto);
        updateResponse.EnsureSuccessStatusCode();

        var updated = await updateResponse.Content.ReadFromJsonAsync<AnimalDto>();
        Assert.Equal("IntNewName", updated!.Name);
        Assert.False(updated.IsAllowed);
    }

    [Fact]
    public async Task DeleteAnimal_ReturnsNoContent()
    {
        var createDto = new CreateAnimalDto("IntToDelete", null, true);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/animals", createDto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AnimalDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/animals/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/animals/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetNonExistentAnimal_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/animals/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDoorStatus_ReturnsConfiguration()
    {
        var response = await _client.GetAsync("/api/v1/doors/status");
        response.EnsureSuccessStatusCode();

        var config = await response.Content.ReadFromJsonAsync<DoorConfigurationDto>();
        Assert.NotNull(config);
    }

    [Fact]
    public async Task UpdateDoorConfiguration_ReturnsUpdated()
    {
        var dto = new UpdateDoorConfigurationDto(false, null, 30, 0.8, null, null, null);
        var response = await _client.PutAsJsonAsync("/api/v1/doors/configuration", dto);
        response.EnsureSuccessStatusCode();

        var config = await response.Content.ReadFromJsonAsync<DoorConfigurationDto>();
        Assert.NotNull(config);
        Assert.False(config.IsEnabled);
        Assert.Equal(30, config.AutoCloseDelaySeconds);
    }

    [Fact]
    public async Task GetAccessLogs_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/v1/accesslogs");
        response.EnsureSuccessStatusCode();
    }
}
