using System.Net;
using System.Net.Http.Json;
using DogDoor.Api.DTOs;
using DogDoor.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DogDoor.Api.Data;
using DogDoor.Api.Models;

namespace DogDoor.Api.Tests.Integration;

public class GeofenceIntegrationTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebAppFactory _factory;
    private const int TestUserId = 200;

    public GeofenceIntegrationTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, TestUserId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DogDoorDbContext>();

        if (!db.Users.Any(u => u.Id == TestUserId))
        {
            db.Users.Add(new User
            {
                Id = TestUserId,
                Email = $"fencetest{TestUserId}@example.com",
                PasswordHash = "dummy-hash"
            });
            db.SaveChanges();
        }
    }

    // ── Geofence CRUD ───────────────────────────────────────

    [Fact]
    public async Task CreateGeofence_ReturnsCreated()
    {
        var dto = new CreateGeofenceDto(
            "Front Yard",
            "polygon",
            "allow",
            "{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[10,0],[10,10],[0,10],[0,0]]]}",
            1
        );

        var response = await _client.PostAsJsonAsync("/api/v1/geofences", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var fence = await response.Content.ReadFromJsonAsync<GeofenceDto>();
        Assert.NotNull(fence);
        Assert.Equal("Front Yard", fence!.Name);
        Assert.Equal("polygon", fence.FenceType);
        Assert.Equal("allow", fence.Rule);
        Assert.Equal(1, fence.Version);
    }

    [Fact]
    public async Task GetGeofences_ReturnsAllForUser()
    {
        await _client.PostAsJsonAsync("/api/v1/geofences", new CreateGeofenceDto(
            "Fence A", "polygon", "allow", "{}", 1));
        await _client.PostAsJsonAsync("/api/v1/geofences", new CreateGeofenceDto(
            "Fence B", "circle", "deny", "{\"center\":[0,0],\"radius\":5}", 2));

        var response = await _client.GetAsync("/api/v1/geofences");
        response.EnsureSuccessStatusCode();
        var fences = await response.Content.ReadFromJsonAsync<GeofenceDto[]>();
        Assert.NotNull(fences);
        Assert.True(fences!.Length >= 2);
    }

    [Fact]
    public async Task UpdateGeofence_IncrementsVersion()
    {
        var createDto = new CreateGeofenceDto("Versioned Fence", "polygon", "allow", "{}", 1);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/geofences", createDto);
        var fence = await createResponse.Content.ReadFromJsonAsync<GeofenceDto>();

        var updateDto = new UpdateGeofenceDto("Renamed Fence", null, null, null, null);
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/geofences/{fence!.Id}", updateDto);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<GeofenceDto>();
        Assert.Equal("Renamed Fence", updated!.Name);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task DeleteGeofence_ReturnsNoContent()
    {
        var createDto = new CreateGeofenceDto("ToDelete", "polygon", "deny", "{}", 1);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/geofences", createDto);
        var fence = await createResponse.Content.ReadFromJsonAsync<GeofenceDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/geofences/{fence!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/geofences/{fence.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetGeofence_OtherUserReturnsNotFound()
    {
        var createDto = new CreateGeofenceDto("OwnerOnly Fence", "polygon", "allow", "{}", 1);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/geofences", createDto);
        var fence = await createResponse.Content.ReadFromJsonAsync<GeofenceDto>();

        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "997");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DogDoorDbContext>();
        if (!db.Users.Any(u => u.Id == 997))
        {
            db.Users.Add(new User { Id = 997, Email = "fence997@example.com", PasswordHash = "h" });
            db.SaveChanges();
        }

        var getResponse = await otherClient.GetAsync($"/api/v1/geofences/{fence!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // ── Geofence Sync ───────────────────────────────────────

    [Fact]
    public async Task SyncGeofences_ReturnsUpdatedFences()
    {
        var createDto = new CreateGeofenceDto("Sync Fence", "polygon", "allow", "{}", 1);
        await _client.PostAsJsonAsync("/api/v1/geofences", createDto);

        var response = await _client.GetAsync($"/api/v1/geofences/sync?userId={TestUserId}&sinceVersion=0");
        response.EnsureSuccessStatusCode();
        var sync = await response.Content.ReadFromJsonAsync<GeofenceSyncDto>();
        Assert.NotNull(sync);
        Assert.True(sync!.Fences.Length > 0);
    }

    // ── Geofence Events ─────────────────────────────────────

    [Fact]
    public async Task RecordGeofenceEvents_ReturnsCreated()
    {
        // Create a collar and a fence
        var collarDto = new CreateCollarDeviceDto("EventTestCollar", null);
        var collarResponse = await _client.PostAsJsonAsync("/api/v1/collars", collarDto);
        var pairing = await collarResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var fenceDto = new CreateGeofenceDto("Event Fence", "polygon", "allow", "{}", 1);
        var fenceResponse = await _client.PostAsJsonAsync("/api/v1/geofences", fenceDto);
        var fence = await fenceResponse.Content.ReadFromJsonAsync<GeofenceDto>();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var eventBatch = new GeofenceEventBatchDto(null, pairing!.CollarId, new[]
        {
            new GeofenceEventInputDto(fence!.Id, "breach", 40.01, -105.27, now)
        });

        var eventResponse = await _client.PostAsJsonAsync("/api/v1/geofences/events", eventBatch);
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
    }

    [Fact]
    public async Task GetGeofenceEvents_ReturnsRecordedEvents()
    {
        // Create collar and fence
        var collarDto = new CreateCollarDeviceDto("EventQueryCollar", null);
        var collarResponse = await _client.PostAsJsonAsync("/api/v1/collars", collarDto);
        var pairing = await collarResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var fenceDto = new CreateGeofenceDto("Event Query Fence", "polygon", "allow", "{}", 1);
        var fenceResponse = await _client.PostAsJsonAsync("/api/v1/geofences", fenceDto);
        var fence = await fenceResponse.Content.ReadFromJsonAsync<GeofenceDto>();

        // Record an event
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _client.PostAsJsonAsync("/api/v1/geofences/events",
            new GeofenceEventBatchDto(null, pairing!.CollarId, new[]
            {
                new GeofenceEventInputDto(fence!.Id, "entered", 40.01, -105.27, now)
            }));

        // Query events
        var response = await _client.GetAsync("/api/v1/geofences/events");
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<GeofenceEventDto[]>();
        Assert.NotNull(events);
        Assert.True(events!.Length > 0);
    }
}
