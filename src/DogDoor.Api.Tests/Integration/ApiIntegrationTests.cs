using System.Net;
using System.Net.Http.Json;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
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
        });
    }
}

public class ApiIntegrationTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAnimals_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/animals");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateAndGetAnimal_RoundTrip()
    {
        var createDto = new CreateAnimalDto("IntBuddy", "Golden Retriever", true);

        var createResponse = await _client.PostAsJsonAsync("/api/animals", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<AnimalDto>();
        Assert.NotNull(created);
        Assert.Equal("IntBuddy", created.Name);

        var getResponse = await _client.GetAsync($"/api/animals/{created.Id}");
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
        var createResponse = await _client.PostAsJsonAsync("/api/animals", createDto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AnimalDto>();

        var updateDto = new UpdateAnimalDto("IntNewName", null, false);
        var updateResponse = await _client.PutAsJsonAsync($"/api/animals/{created!.Id}", updateDto);
        updateResponse.EnsureSuccessStatusCode();

        var updated = await updateResponse.Content.ReadFromJsonAsync<AnimalDto>();
        Assert.Equal("IntNewName", updated!.Name);
        Assert.False(updated.IsAllowed);
    }

    [Fact]
    public async Task DeleteAnimal_ReturnsNoContent()
    {
        var createDto = new CreateAnimalDto("IntToDelete", null, true);
        var createResponse = await _client.PostAsJsonAsync("/api/animals", createDto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AnimalDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/animals/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/animals/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetNonExistentAnimal_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/animals/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDoorStatus_ReturnsConfiguration()
    {
        var response = await _client.GetAsync("/api/doors/status");
        response.EnsureSuccessStatusCode();

        var config = await response.Content.ReadFromJsonAsync<DoorConfigurationDto>();
        Assert.NotNull(config);
    }

    [Fact]
    public async Task UpdateDoorConfiguration_ReturnsUpdated()
    {
        var dto = new UpdateDoorConfigurationDto(false, null, 30, 0.8, null, null, null);
        var response = await _client.PutAsJsonAsync("/api/doors/configuration", dto);
        response.EnsureSuccessStatusCode();

        var config = await response.Content.ReadFromJsonAsync<DoorConfigurationDto>();
        Assert.NotNull(config);
        Assert.False(config.IsEnabled);
        Assert.Equal(30, config.AutoCloseDelaySeconds);
    }

    [Fact]
    public async Task GetAccessLogs_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/accesslogs");
        response.EnsureSuccessStatusCode();
    }
}
