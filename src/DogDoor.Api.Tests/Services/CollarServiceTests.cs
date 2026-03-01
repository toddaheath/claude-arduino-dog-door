using System.Security.Cryptography;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using DogDoor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Tests.Services;

public class CollarServiceTests : IDisposable
{
    private readonly DogDoorDbContext _db;
    private readonly CollarService _service;
    private const int UserId = 50;

    public CollarServiceTests()
    {
        var options = new DbContextOptionsBuilder<DogDoorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new DogDoorDbContext(options);
        _service = new CollarService(_db);

        // Seed user + animal
        _db.Users.Add(new User { Id = UserId, Email = "collar@test.com", PasswordHash = "h" });
        _db.Animals.Add(new Animal { Id = 1, UserId = UserId, Name = "Rex", IsAllowed = true });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // ── Registration ─────────────────────────────────────

    [Fact]
    public async Task RegisterCollar_GeneratesUniqueIdAndSecret()
    {
        var result = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("Collar A", null));

        Assert.NotEmpty(result.CollarId);
        Assert.Equal(16, result.CollarId.Length);
        Assert.NotEmpty(result.SharedSecret);
        Assert.Equal("Collar A", result.Name);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task RegisterCollar_WithAnimalId_LinksAnimal()
    {
        var result = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("Dog Collar", 1));

        var collar = await _db.CollarDevices.FindAsync(result.Id);
        Assert.Equal(1, collar!.AnimalId);
    }

    // ── CRUD ─────────────────────────────────────────────

    [Fact]
    public async Task GetCollar_ReturnsNullForOtherUser()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("MyCollar", null));

        var result = await _service.GetCollarAsync(999, pairing.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCollar_ChangesFields()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("OldName", null));

        var updated = await _service.UpdateCollarAsync(UserId, pairing.Id,
            new UpdateCollarDeviceDto("NewName", 1, false));

        Assert.NotNull(updated);
        Assert.Equal("NewName", updated!.Name);
        Assert.Equal(1, updated.AnimalId);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeleteCollar_RemovesFromDb()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("ToDelete", null));

        var deleted = await _service.DeleteCollarAsync(UserId, pairing.Id);

        Assert.True(deleted);
        Assert.Null(await _db.CollarDevices.FindAsync(pairing.Id));
    }

    // ── NFC Verification ─────────────────────────────────

    [Fact]
    public async Task VerifyNfc_ValidHmac_ReturnsVerified()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("NfcCollar", 1));

        var secretBytes = Convert.FromBase64String(pairing.SharedSecret);
        var challenge = "aabbccddee112233";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var challengeBytes = Convert.FromHexString(challenge);
        var timestampBytes = BitConverter.GetBytes(timestamp);
        var payload = new byte[challengeBytes.Length + timestampBytes.Length];
        Buffer.BlockCopy(challengeBytes, 0, payload, 0, challengeBytes.Length);
        Buffer.BlockCopy(timestampBytes, 0, payload, challengeBytes.Length, timestampBytes.Length);

        using var hmac = new HMACSHA256(secretBytes);
        var responseHex = Convert.ToHexString(hmac.ComputeHash(payload)).ToLower();

        var result = await _service.VerifyNfcAsync(pairing.CollarId,
            new NfcVerifyRequestDto(pairing.CollarId, challenge, responseHex, timestamp));

        Assert.True(result.Verified);
        Assert.Equal(1, result.AnimalId);
        Assert.Equal("Rex", result.AnimalName);
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task VerifyNfc_WrongHmac_Fails()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("BadNfc", null));

        var result = await _service.VerifyNfcAsync(pairing.CollarId,
            new NfcVerifyRequestDto(pairing.CollarId, "aabb", "wronghmac", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        Assert.False(result.Verified);
        Assert.Equal("HMAC verification failed", result.Reason);
    }

    [Fact]
    public async Task VerifyNfc_ExpiredTimestamp_Fails()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("Expired", null));

        var result = await _service.VerifyNfcAsync(pairing.CollarId,
            new NfcVerifyRequestDto(pairing.CollarId, "aabb", "ccdd",
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60));

        Assert.False(result.Verified);
        Assert.Equal("Timestamp expired", result.Reason);
    }

    [Fact]
    public async Task VerifyNfc_InactiveCollar_Fails()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("Inactive", null));

        // Deactivate
        var collar = await _db.CollarDevices.FindAsync(pairing.Id);
        collar!.IsActive = false;
        await _db.SaveChangesAsync();

        var result = await _service.VerifyNfcAsync(pairing.CollarId,
            new NfcVerifyRequestDto(pairing.CollarId, "aabb", "ccdd", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        Assert.False(result.Verified);
        Assert.Equal("Collar not found", result.Reason);
    }

    // ── Location Upload ──────────────────────────────────

    [Fact]
    public async Task UploadLocations_StoresPointsAndUpdatesCollar()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("LocCollar", null));

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var points = new[]
        {
            new LocationPointDto(40.0, -105.0, null, 5.0f, null, null, null, 3.9f, now - 10),
            new LocationPointDto(40.1, -105.1, null, 3.0f, null, null, null, 3.8f, now),
        };

        var count = await _service.UploadLocationsAsync(pairing.CollarId, points);

        Assert.Equal(2, count);

        var collar = await _db.CollarDevices.FindAsync(pairing.Id);
        Assert.Equal(40.1, collar!.LastLatitude!.Value, 1);
        Assert.Equal(-105.1, collar.LastLongitude!.Value, 1);
        Assert.Equal(3.8f, collar.BatteryVoltage);
    }

    [Fact]
    public async Task UploadLocations_UnknownCollar_ReturnsZero()
    {
        var count = await _service.UploadLocationsAsync("nonexistent", new[]
        {
            new LocationPointDto(40.0, -105.0, null, null, null, null, null, null, 0)
        });

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetCurrentLocation_ReturnsLatestData()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("CurLoc", null));

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _service.UploadLocationsAsync(pairing.CollarId, new[]
        {
            new LocationPointDto(42.0, -106.0, null, 2.5f, null, null, null, null, now),
        });

        var loc = await _service.GetCurrentLocationAsync(pairing.Id);

        Assert.NotNull(loc);
        Assert.Equal(42.0, loc!.Latitude, 1);
        Assert.Equal(-106.0, loc.Longitude, 1);
    }

    [Fact]
    public async Task GetCurrentLocation_NoData_ReturnsNull()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("NoLoc", null));

        var loc = await _service.GetCurrentLocationAsync(pairing.Id);

        Assert.Null(loc);
    }

    // ── Firmware Management ───────────────────────────────

    [Fact]
    public async Task CheckFirmware_NoReleases_ReturnsNotAvailable()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("FwCheck", null));

        var result = await _service.CheckFirmwareAsync(pairing.CollarId, "1.0.0");

        Assert.False(result.UpdateAvailable);
        Assert.Null(result.LatestVersion);
    }

    [Fact]
    public async Task CheckFirmware_NewerRelease_ReturnsAvailable()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("FwNew", null));

        _db.FirmwareReleases.Add(new FirmwareRelease
        {
            Version = "2.0.0",
            FilePath = "/tmp/firmware_2.0.0.bin",
            FileSize = 50000,
            ReleaseNotes = "New features",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var result = await _service.CheckFirmwareAsync(pairing.CollarId, "1.0.0");

        Assert.True(result.UpdateAvailable);
        Assert.Equal("2.0.0", result.LatestVersion);
        Assert.Equal(50000, result.FileSize);
        Assert.Equal("New features", result.ReleaseNotes);
    }

    [Fact]
    public async Task CheckFirmware_SameVersion_ReturnsNotAvailable()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("FwSame", null));

        _db.FirmwareReleases.Add(new FirmwareRelease
        {
            Version = "1.0.0",
            FilePath = "/tmp/firmware_1.0.0.bin",
            FileSize = 40000,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var result = await _service.CheckFirmwareAsync(pairing.CollarId, "1.0.0");

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckFirmware_UpdatesCollarFirmwareVersion()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("FwVer", null));

        await _service.CheckFirmwareAsync(pairing.CollarId, "1.5.0");

        var collar = await _db.CollarDevices.FindAsync(pairing.Id);
        Assert.Equal("1.5.0", collar!.FirmwareVersion);
    }

    // ── Activity Summary ───────────────────────────────────

    [Fact]
    public async Task GetActivitySummary_WithPoints_ComputesMetrics()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("ActivityCollar", null));

        var baseTime = DateTime.UtcNow.AddHours(-1);
        for (int i = 0; i < 10; i++)
        {
            _db.LocationPoints.Add(new LocationPoint
            {
                CollarDeviceId = pairing.Id,
                Latitude = 40.0 + (i * 0.001),
                Longitude = -105.0,
                Speed = 1.5f,
                Timestamp = baseTime.AddMinutes(i * 5),
            });
        }
        await _db.SaveChangesAsync();

        var summary = await _service.GetActivitySummaryAsync(UserId, pairing.Id,
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);

        Assert.NotNull(summary);
        Assert.Equal(10, summary!.LocationPointCount);
        Assert.True(summary.TotalDistanceMeters > 500);
        Assert.True(summary.ActiveMinutes > 0);
        Assert.Equal(1.5, summary.MaxSpeedMps, 1);
    }

    [Fact]
    public async Task GetActivitySummary_NoPoints_ReturnsZeros()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("EmptyActivity", null));

        var summary = await _service.GetActivitySummaryAsync(UserId, pairing.Id,
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);

        Assert.NotNull(summary);
        Assert.Equal(0, summary!.LocationPointCount);
        Assert.Equal(0, summary.TotalDistanceMeters);
        Assert.Equal(0, summary.ActiveMinutes);
    }

    [Fact]
    public async Task GetActivitySummary_OtherUser_ReturnsNull()
    {
        var pairing = await _service.RegisterCollarAsync(UserId, new CreateCollarDeviceDto("OtherActivity", null));

        var summary = await _service.GetActivitySummaryAsync(999, pairing.Id,
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);

        Assert.Null(summary);
    }

    [Fact]
    public async Task GetFirmwareReleases_ReturnsAllReleases()
    {
        _db.FirmwareReleases.Add(new FirmwareRelease
        {
            Version = "1.0.0", FilePath = "/tmp/a.bin", FileSize = 1000, IsActive = true
        });
        _db.FirmwareReleases.Add(new FirmwareRelease
        {
            Version = "1.1.0", FilePath = "/tmp/b.bin", FileSize = 2000, IsActive = true
        });
        await _db.SaveChangesAsync();

        var releases = (await _service.GetFirmwareReleasesAsync()).ToList();

        Assert.Equal(2, releases.Count);
    }
}
