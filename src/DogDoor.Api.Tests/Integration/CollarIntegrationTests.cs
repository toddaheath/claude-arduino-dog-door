using System.Net;
using System.Net.Http.Json;
using DogDoor.Api.DTOs;
using DogDoor.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DogDoor.Api.Data;
using DogDoor.Api.Models;

namespace DogDoor.Api.Tests.Integration;

public class CollarIntegrationTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebAppFactory _factory;
    private const int TestUserId = 100;

    public CollarIntegrationTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, TestUserId.ToString());

        // Seed test user and animal
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DogDoorDbContext>();

        if (!db.Users.Any(u => u.Id == TestUserId))
        {
            db.Users.Add(new User
            {
                Id = TestUserId,
                Email = $"collartest{TestUserId}@example.com",
                PasswordHash = "dummy-hash"
            });
            db.SaveChanges();
        }

        if (!db.Animals.Any(a => a.UserId == TestUserId))
        {
            db.Animals.Add(new Animal
            {
                UserId = TestUserId,
                Name = "TestDog",
                Breed = "Lab",
                IsAllowed = true
            });
            db.SaveChanges();
        }
    }

    // ── Collar CRUD ─────────────────────────────────────────

    [Fact]
    public async Task RegisterCollar_ReturnsCreatedWithPairingCredentials()
    {
        var dto = new CreateCollarDeviceDto("Buddy's Collar", null);
        var response = await _client.PostAsJsonAsync("/api/v1/collars", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CollarPairingResultDto>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.CollarId);
        Assert.NotEmpty(result.SharedSecret);
        Assert.Equal("Buddy's Collar", result.Name);
    }

    [Fact]
    public async Task GetCollars_ReturnsEmptyForNewUser()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "999");

        // Seed user 999
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DogDoorDbContext>();
        if (!db.Users.Any(u => u.Id == 999))
        {
            db.Users.Add(new User { Id = 999, Email = "collar999@example.com", PasswordHash = "h" });
            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/v1/collars");
        response.EnsureSuccessStatusCode();
        var collars = await response.Content.ReadFromJsonAsync<CollarDeviceDto[]>();
        Assert.NotNull(collars);
        Assert.Empty(collars!);
    }

    [Fact]
    public async Task RegisterAndGetCollar_RoundTrips()
    {
        var dto = new CreateCollarDeviceDto("RoundTrip Collar", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        createResponse.EnsureSuccessStatusCode();
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var getResponse = await _client.GetAsync($"/api/v1/collars/{pairing!.Id}");
        getResponse.EnsureSuccessStatusCode();
        var collar = await getResponse.Content.ReadFromJsonAsync<CollarDeviceDto>();
        Assert.NotNull(collar);
        Assert.Equal("RoundTrip Collar", collar!.Name);
        Assert.Equal(pairing.CollarId, collar.CollarId);
        Assert.True(collar.IsActive);
    }

    [Fact]
    public async Task UpdateCollar_ChangesName()
    {
        var dto = new CreateCollarDeviceDto("OldName", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var updateDto = new UpdateCollarDeviceDto("NewName", null, null);
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/collars/{pairing!.Id}", updateDto);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CollarDeviceDto>();
        Assert.Equal("NewName", updated!.Name);
    }

    [Fact]
    public async Task DeleteCollar_ReturnsNoContent()
    {
        var dto = new CreateCollarDeviceDto("ToDelete", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/collars/{pairing!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/collars/{pairing.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetCollar_OtherUserReturnsNotFound()
    {
        var dto = new CreateCollarDeviceDto("OwnerOnly", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        // Different user
        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "998");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DogDoorDbContext>();
        if (!db.Users.Any(u => u.Id == 998))
        {
            db.Users.Add(new User { Id = 998, Email = "collar998@example.com", PasswordHash = "h" });
            db.SaveChanges();
        }

        var getResponse = await otherClient.GetAsync($"/api/v1/collars/{pairing!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // ── Location Upload ─────────────────────────────────────

    [Fact]
    public async Task UploadLocations_StoresPointsAndUpdatesCollar()
    {
        var dto = new CreateCollarDeviceDto("LocationCollar", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var batch = new LocationBatchDto(null, new[]
        {
            new LocationPointDto(40.01, -105.27, 1600, 2.5f, 1.2f, 90, 8, 3.9f, now - 2),
            new LocationPointDto(40.02, -105.28, 1601, 2.0f, 1.5f, 85, 9, 3.85f, now - 1),
            new LocationPointDto(40.03, -105.29, 1602, 1.8f, 0.5f, 80, 10, 3.8f, now)
        });

        var uploadResponse = await _client.PostAsJsonAsync(
            $"/api/v1/collars/{pairing!.CollarId}/locations", batch);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        // Verify collar was updated with latest position
        var getResponse = await _client.GetAsync($"/api/v1/collars/{pairing.Id}");
        var collar = await getResponse.Content.ReadFromJsonAsync<CollarDeviceDto>();
        Assert.NotNull(collar!.LastLatitude);
        Assert.Equal(40.03, collar.LastLatitude!.Value, 2);
    }

    [Fact]
    public async Task GetLocations_ReturnsUploadedPoints()
    {
        var dto = new CreateCollarDeviceDto("HistoryCollar", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var batch = new LocationBatchDto(null, new[]
        {
            new LocationPointDto(41.0, -106.0, null, null, null, null, null, null, now)
        });

        await _client.PostAsJsonAsync($"/api/v1/collars/{pairing!.CollarId}/locations", batch);

        var historyResponse = await _client.GetAsync($"/api/v1/collars/{pairing.Id}/locations");
        historyResponse.EnsureSuccessStatusCode();
        var points = await historyResponse.Content.ReadFromJsonAsync<LocationQueryDto[]>();
        Assert.NotNull(points);
        Assert.NotEmpty(points!);
    }

    [Fact]
    public async Task GetCurrentLocation_ReturnsLatestPosition()
    {
        var dto = new CreateCollarDeviceDto("CurrentLocCollar", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var batch = new LocationBatchDto(null, new[]
        {
            new LocationPointDto(42.5, -107.5, null, 3.0f, null, null, null, null, now)
        });

        await _client.PostAsJsonAsync($"/api/v1/collars/{pairing!.CollarId}/locations", batch);

        var currentResponse = await _client.GetAsync($"/api/v1/collars/{pairing.Id}/location/current");
        currentResponse.EnsureSuccessStatusCode();
        var current = await currentResponse.Content.ReadFromJsonAsync<CurrentLocationDto>();
        Assert.NotNull(current);
        Assert.Equal(42.5, current!.Latitude, 2);
    }

    // ── NFC Verify ──────────────────────────────────────────

    [Fact]
    public async Task VerifyNfc_WithInvalidCollar_ReturnsFalse()
    {
        var nfcDto = new NfcVerifyRequestDto(
            "nonexistent",
            "aabbccdd",
            "deadbeef",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );

        var response = await _client.PostAsJsonAsync("/api/v1/collars/nonexistent/verify", nfcDto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NfcVerifyResponseDto>();
        Assert.False(result!.Verified);
        Assert.Equal("Collar not found", result.Reason);
    }

    [Fact]
    public async Task VerifyNfc_WithExpiredTimestamp_ReturnsFalse()
    {
        var dto = new CreateCollarDeviceDto("NfcTestCollar", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var nfcDto = new NfcVerifyRequestDto(
            pairing!.CollarId,
            "aabbccdd",
            "deadbeef",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60 // 60s ago (outside 30s window)
        );

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/collars/{pairing.CollarId}/verify", nfcDto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NfcVerifyResponseDto>();
        Assert.False(result!.Verified);
        Assert.Equal("Timestamp expired", result.Reason);
    }

    [Fact]
    public async Task VerifyNfc_WithValidHmac_ReturnsTrue()
    {
        var dto = new CreateCollarDeviceDto("ValidNfcCollar", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        // Compute valid HMAC
        var secretBytes = Convert.FromBase64String(pairing!.SharedSecret);
        var challenge = "aabbccddee112233";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var challengeBytes = Convert.FromHexString(challenge);
        var timestampBytes = BitConverter.GetBytes(timestamp);
        var payload = new byte[challengeBytes.Length + timestampBytes.Length];
        Buffer.BlockCopy(challengeBytes, 0, payload, 0, challengeBytes.Length);
        Buffer.BlockCopy(timestampBytes, 0, payload, challengeBytes.Length, timestampBytes.Length);

        using var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes);
        var responseHex = Convert.ToHexString(hmac.ComputeHash(payload)).ToLower();

        var nfcDto = new NfcVerifyRequestDto(pairing.CollarId, challenge, responseHex, timestamp);
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/collars/{pairing.CollarId}/verify", nfcDto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NfcVerifyResponseDto>();
        Assert.True(result!.Verified);
    }

    // ── Firmware Management ──────────────────────────────────

    [Fact]
    public async Task CheckFirmware_NoRelease_ReturnsNotFound()
    {
        var dto = new CreateCollarDeviceDto("FwCheckCollar", null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/collars", dto);
        var pairing = await createResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        var response = await _client.GetAsync(
            $"/api/v1/collars/{pairing!.CollarId}/firmware?current=1.0.0");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadFirmware_ThenCheckAvailable()
    {
        // Upload firmware
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("2.0.0"), "version");
        content.Add(new StringContent("Bug fixes"), "releaseNotes");
        var firmwareData = new byte[1024]; // Dummy firmware binary
        new Random(42).NextBytes(firmwareData);
        content.Add(new ByteArrayContent(firmwareData), "file", "collar_2.0.0.bin");

        var uploadResponse = await _client.PostAsync("/api/v1/collars/firmware", content);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var release = await uploadResponse.Content.ReadFromJsonAsync<FirmwareReleaseDto>();
        Assert.NotNull(release);
        Assert.Equal("2.0.0", release!.Version);
        Assert.Equal(1024, release.FileSize);
        Assert.NotNull(release.Sha256Hash);

        // Register a collar
        var collarDto = new CreateCollarDeviceDto("FwTestCollar", null);
        var collarResponse = await _client.PostAsJsonAsync("/api/v1/collars", collarDto);
        var pairing = await collarResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        // Check firmware — should see update available
        var checkResponse = await _client.GetAsync(
            $"/api/v1/collars/{pairing!.CollarId}/firmware?current=1.0.0");
        Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);
        var check = await checkResponse.Content.ReadFromJsonAsync<FirmwareCheckDto>();
        Assert.True(check!.UpdateAvailable);
        Assert.Equal("2.0.0", check.LatestVersion);
    }

    [Fact]
    public async Task UploadFirmware_SameVersion_CheckReturnsNotFound()
    {
        // Upload firmware v3.0.0
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("3.0.0"), "version");
        var firmwareData = new byte[512];
        content.Add(new ByteArrayContent(firmwareData), "file", "collar_3.0.0.bin");
        await _client.PostAsync("/api/v1/collars/firmware", content);

        // Register collar
        var collarDto = new CreateCollarDeviceDto("SameVersionCollar", null);
        var collarResponse = await _client.PostAsJsonAsync("/api/v1/collars", collarDto);
        var pairing = await collarResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        // Check with same version — no update
        var checkResponse = await _client.GetAsync(
            $"/api/v1/collars/{pairing!.CollarId}/firmware?current=3.0.0");
        Assert.Equal(HttpStatusCode.NotFound, checkResponse.StatusCode);
    }

    [Fact]
    public async Task DownloadFirmware_WithRelease_ReturnsFile()
    {
        // Upload firmware v4.0.0
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("4.0.0"), "version");
        var firmwareData = new byte[2048];
        new Random(99).NextBytes(firmwareData);
        content.Add(new ByteArrayContent(firmwareData), "file", "collar_4.0.0.bin");
        await _client.PostAsync("/api/v1/collars/firmware", content);

        // Register collar
        var collarDto = new CreateCollarDeviceDto("DownloadCollar", null);
        var collarResponse = await _client.PostAsJsonAsync("/api/v1/collars", collarDto);
        var pairing = await collarResponse.Content.ReadFromJsonAsync<CollarPairingResultDto>();

        // Download firmware
        var downloadResponse = await _client.GetAsync(
            $"/api/v1/collars/{pairing!.CollarId}/firmware/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/octet-stream", downloadResponse.Content.Headers.ContentType?.MediaType);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(2048, downloadedBytes.Length);
    }

    [Fact]
    public async Task GetFirmwareReleases_ReturnsUploadedReleases()
    {
        var response = await _client.GetAsync("/api/v1/collars/firmware");
        response.EnsureSuccessStatusCode();
        var releases = await response.Content.ReadFromJsonAsync<FirmwareReleaseDto[]>();
        Assert.NotNull(releases);
    }
}
