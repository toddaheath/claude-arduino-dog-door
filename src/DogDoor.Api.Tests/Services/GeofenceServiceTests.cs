using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DogDoor.Api.Tests.Services;

public class GeofenceServiceTests : IDisposable
{
    private readonly DogDoorDbContext _db;
    private readonly GeofenceService _service;
    private readonly Mock<INotificationService> _notificationMock;
    private const int UserId = 60;

    public GeofenceServiceTests()
    {
        var options = new DbContextOptionsBuilder<DogDoorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new DogDoorDbContext(options);
        _notificationMock = new Mock<INotificationService>();
        _service = new GeofenceService(_db, _notificationMock.Object);

        // Seed user
        _db.Users.Add(new User { Id = UserId, Email = "fence@test.com", PasswordHash = "h" });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // ── CRUD ─────────────────────────────────────────────

    [Fact]
    public async Task CreateGeofence_SetsVersionToOne()
    {
        var fence = await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("Yard", "polygon", "allow", "{}", 1));

        Assert.Equal(1, fence.Version);
        Assert.True(fence.IsActive);
        Assert.Equal("Yard", fence.Name);
    }

    [Fact]
    public async Task UpdateGeofence_IncrementsVersion()
    {
        var fence = await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("Yard", "polygon", "allow", "{}", 1));

        var updated = await _service.UpdateGeofenceAsync(UserId, fence.Id,
            new UpdateGeofenceDto("Big Yard", null, null, null, null));

        Assert.Equal(2, updated!.Version);
        Assert.Equal("Big Yard", updated.Name);
    }

    [Fact]
    public async Task UpdateGeofence_OtherUser_ReturnsNull()
    {
        var fence = await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("Yard", "polygon", "allow", "{}", 1));

        var result = await _service.UpdateGeofenceAsync(999, fence.Id,
            new UpdateGeofenceDto("Hacked", null, null, null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteGeofence_RemovesFromDb()
    {
        var fence = await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("ToDelete", "polygon", "deny", "{}", 1));

        var deleted = await _service.DeleteGeofenceAsync(UserId, fence.Id);

        Assert.True(deleted);
        var found = await _db.Geofences.FindAsync(fence.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteGeofence_OtherUser_ReturnsFalse()
    {
        var fence = await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("Protected", "polygon", "allow", "{}", 1));

        var deleted = await _service.DeleteGeofenceAsync(999, fence.Id);

        Assert.False(deleted);
    }

    // ── Sync Protocol ────────────────────────────────────

    [Fact]
    public async Task Sync_ReturnsOnlyNewerVersions()
    {
        var f1 = await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("V1", "polygon", "allow", "{}", 1));

        // Update to get version 2
        await _service.UpdateGeofenceAsync(UserId, f1.Id,
            new UpdateGeofenceDto("V2", null, null, null, null));

        var f2 = await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("AnotherFence", "circle", "deny", "{}", 2));

        // Since version 1 should return f1 (now v2) and f2 (v1)
        var sync = await _service.GetGeofenceSyncAsync(UserId, 1);

        Assert.Equal(2, sync.Version);
        Assert.Single(sync.Fences); // Only f1 at v2 is > sinceVersion=1
    }

    [Fact]
    public async Task Sync_NoUpdates_ReturnsEmptyWithSameVersion()
    {
        await _service.CreateGeofenceAsync(UserId,
            new CreateGeofenceDto("Static", "polygon", "allow", "{}", 1));

        var sync = await _service.GetGeofenceSyncAsync(UserId, 99);

        Assert.Empty(sync.Fences);
        Assert.Equal(99, sync.Version);
    }

    // ── Event Recording ──────────────────────────────────

    [Fact]
    public async Task RecordEvents_StoresEventsAndNotifiesOnBreach()
    {
        // Setup collar and fence
        _db.CollarDevices.Add(new CollarDevice
        {
            UserId = UserId,
            CollarId = "testcollar1",
            Name = "TestCollar",
            SharedSecret = Convert.ToBase64String(new byte[32]),
        });
        var fence = new Geofence
        {
            UserId = UserId,
            Name = "Safe Zone",
            FenceType = "polygon",
            Rule = "allow",
            BoundaryJson = "{}",
            BuzzerPattern = 1,
            IsActive = true,
            Version = 1,
        };
        _db.Geofences.Add(fence);
        await _db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var events = new[]
        {
            new GeofenceEventInputDto(fence.Id, "breach", 40.0, -105.0, now),
        };

        var count = await _service.RecordGeofenceEventsAsync("testcollar1", events);

        Assert.Equal(1, count);

        // Verify notification was sent for breach
        _notificationMock.Verify(n => n.NotifyAsync(
            UserId,
            DoorEventType.GeofenceBreach,
            It.IsAny<string?>(),
            It.IsAny<DoorSide?>(),
            "Safe Zone"
        ), Times.Once);
    }

    [Fact]
    public async Task RecordEvents_NonBreachType_DoesNotNotify()
    {
        _db.CollarDevices.Add(new CollarDevice
        {
            UserId = UserId,
            CollarId = "testcollar2",
            Name = "Collar2",
            SharedSecret = Convert.ToBase64String(new byte[32]),
        });
        var fence = new Geofence
        {
            UserId = UserId,
            Name = "Park",
            FenceType = "circle",
            Rule = "allow",
            BoundaryJson = "{}",
            BuzzerPattern = 0,
            IsActive = true,
            Version = 1,
        };
        _db.Geofences.Add(fence);
        await _db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _service.RecordGeofenceEventsAsync("testcollar2", new[]
        {
            new GeofenceEventInputDto(fence.Id, "entered", 40.0, -105.0, now),
        });

        _notificationMock.Verify(n => n.NotifyAsync(
            It.IsAny<int>(),
            It.IsAny<DoorEventType>(),
            It.IsAny<string?>(),
            It.IsAny<DoorSide?>(),
            It.IsAny<string?>()
        ), Times.Never);
    }

    [Fact]
    public async Task RecordEvents_UnknownCollar_ReturnsZero()
    {
        var count = await _service.RecordGeofenceEventsAsync("nobody", new[]
        {
            new GeofenceEventInputDto(1, "breach", 40.0, -105.0, 0),
        });

        Assert.Equal(0, count);
    }
}
