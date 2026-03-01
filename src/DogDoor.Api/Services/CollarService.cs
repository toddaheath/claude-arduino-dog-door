using System.Security.Cryptography;
using System.Text;
using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class CollarService : ICollarService
{
    private readonly DogDoorDbContext _db;

    public CollarService(DogDoorDbContext db)
    {
        _db = db;
    }

    public async Task<CollarPairingResultDto> RegisterCollarAsync(int userId, CreateCollarDeviceDto dto)
    {
        var collarId = Guid.NewGuid().ToString("N")[..16];
        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var secretBase64 = Convert.ToBase64String(secretBytes);

        var collar = new CollarDevice
        {
            UserId = userId,
            AnimalId = dto.AnimalId,
            CollarId = collarId,
            Name = dto.Name,
            SharedSecret = secretBase64,
            IsActive = true
        };

        _db.CollarDevices.Add(collar);
        await _db.SaveChangesAsync();

        return new CollarPairingResultDto(collar.Id, collarId, secretBase64, collar.Name);
    }

    public async Task<IEnumerable<CollarDeviceDto>> GetCollarsAsync(int userId)
    {
        return await _db.CollarDevices
            .Where(c => c.UserId == userId)
            .Include(c => c.Animal)
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<CollarDeviceDto?> GetCollarAsync(int userId, int collarId)
    {
        var collar = await _db.CollarDevices
            .Include(c => c.Animal)
            .FirstOrDefaultAsync(c => c.Id == collarId && c.UserId == userId);

        return collar == null ? null : ToDto(collar);
    }

    public async Task<CollarDeviceDto?> UpdateCollarAsync(int userId, int collarId, UpdateCollarDeviceDto dto)
    {
        var collar = await _db.CollarDevices
            .Include(c => c.Animal)
            .FirstOrDefaultAsync(c => c.Id == collarId && c.UserId == userId);

        if (collar == null) return null;

        if (dto.Name != null) collar.Name = dto.Name;
        if (dto.AnimalId != null) collar.AnimalId = dto.AnimalId;
        if (dto.IsActive != null) collar.IsActive = dto.IsActive.Value;
        collar.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(collar);
    }

    public async Task<bool> DeleteCollarAsync(int userId, int collarId)
    {
        var collar = await _db.CollarDevices
            .FirstOrDefaultAsync(c => c.Id == collarId && c.UserId == userId);

        if (collar == null) return false;

        _db.CollarDevices.Remove(collar);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<NfcVerifyResponseDto> VerifyNfcAsync(string collarId, NfcVerifyRequestDto dto)
    {
        var collar = await _db.CollarDevices
            .Include(c => c.Animal)
            .FirstOrDefaultAsync(c => c.CollarId == collarId && c.IsActive);

        if (collar == null)
            return new NfcVerifyResponseDto(false, null, null, null, "Collar not found");

        // Validate timestamp (30-second replay window)
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - dto.Timestamp) > 30)
            return new NfcVerifyResponseDto(false, null, null, null, "Timestamp expired");

        // Verify HMAC-SHA256
        var secretBytes = Convert.FromBase64String(collar.SharedSecret);
        var challengeBytes = Convert.FromHexString(dto.Challenge);
        var timestampBytes = BitConverter.GetBytes(dto.Timestamp);

        var payload = new byte[challengeBytes.Length + timestampBytes.Length];
        Buffer.BlockCopy(challengeBytes, 0, payload, 0, challengeBytes.Length);
        Buffer.BlockCopy(timestampBytes, 0, payload, challengeBytes.Length, timestampBytes.Length);

        using var hmac = new HMACSHA256(secretBytes);
        var expectedResponse = Convert.ToHexString(hmac.ComputeHash(payload)).ToLower();

        // Constant-time comparison
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedResponse),
                Encoding.UTF8.GetBytes(dto.Response.ToLower())))
        {
            return new NfcVerifyResponseDto(false, null, null, null, "HMAC verification failed");
        }

        // Update last seen
        collar.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new NfcVerifyResponseDto(
            true,
            collar.AnimalId,
            collar.Animal?.Name,
            collar.Animal?.IsAllowed,
            null
        );
    }

    public async Task<int> UploadLocationsAsync(string collarId, LocationPointDto[] points)
    {
        var collar = await _db.CollarDevices
            .FirstOrDefaultAsync(c => c.CollarId == collarId);

        if (collar == null) return 0;

        var locationPoints = points.Select(p => new LocationPoint
        {
            CollarDeviceId = collar.Id,
            Latitude = p.Lat,
            Longitude = p.Lng,
            Altitude = p.Alt,
            Accuracy = p.Acc,
            Speed = p.Spd,
            Heading = p.Hdg,
            Satellites = p.Sat,
            BatteryVoltage = p.Bat,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(p.Ts).UtcDateTime
        }).ToList();

        _db.LocationPoints.AddRange(locationPoints);

        // Update collar's last known position from most recent point
        var latest = points.OrderByDescending(p => p.Ts).First();
        collar.LastLatitude = latest.Lat;
        collar.LastLongitude = latest.Lng;
        collar.LastAccuracy = latest.Acc;
        collar.BatteryVoltage = latest.Bat;
        collar.BatteryPercent = latest.Bat.HasValue ? VoltageToPercent(latest.Bat.Value) : null;
        collar.LastSeenAt = DateTimeOffset.FromUnixTimeSeconds(latest.Ts).UtcDateTime;

        await _db.SaveChangesAsync();
        return locationPoints.Count;
    }

    public async Task<IEnumerable<LocationQueryDto>> GetLocationHistoryAsync(
        int collarId, DateTime from, DateTime to)
    {
        return await _db.LocationPoints
            .Where(p => p.CollarDeviceId == collarId && p.Timestamp >= from && p.Timestamp <= to)
            .OrderBy(p => p.Timestamp)
            .Select(p => new LocationQueryDto(
                p.Latitude, p.Longitude, p.Altitude, p.Accuracy,
                p.Speed, p.Heading, p.Satellites, p.Timestamp))
            .ToListAsync();
    }

    public async Task<CurrentLocationDto?> GetCurrentLocationAsync(int collarId)
    {
        var collar = await _db.CollarDevices
            .FirstOrDefaultAsync(c => c.Id == collarId);

        if (collar?.LastLatitude == null || collar.LastLongitude == null)
            return null;

        return new CurrentLocationDto(
            collar.LastLatitude.Value,
            collar.LastLongitude.Value,
            collar.LastAccuracy,
            null,
            collar.LastSeenAt ?? DateTime.UtcNow,
            collar.BatteryPercent,
            null
        );
    }

    public async Task<ActivitySummaryDto?> GetActivitySummaryAsync(int userId, int collarId, DateTime from, DateTime to)
    {
        var collar = await _db.CollarDevices.FirstOrDefaultAsync(c => c.Id == collarId && c.UserId == userId);
        if (collar == null) return null;

        var points = await _db.LocationPoints
            .Where(p => p.CollarDeviceId == collarId && p.Timestamp >= from && p.Timestamp <= to)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();

        if (points.Count == 0)
            return new ActivitySummaryDto(0, 0, 0, 0, 0, from, to);

        double totalDistance = 0;
        double maxSpeed = 0;
        double speedSum = 0;
        int speedCount = 0;
        int activeMinutes = 0;
        DateTime? lastActiveMinute = null;

        for (int i = 1; i < points.Count; i++)
        {
            var prev = points[i - 1];
            var curr = points[i];

            // Haversine distance
            totalDistance += HaversineDistance(prev.Latitude, prev.Longitude, curr.Latitude, curr.Longitude);

            if (curr.Speed.HasValue && curr.Speed.Value > 0.2f)
            {
                speedSum += curr.Speed.Value;
                speedCount++;
                if (curr.Speed.Value > maxSpeed) maxSpeed = curr.Speed.Value;

                // Count active minutes (distinct minutes where speed > 0.2 m/s)
                var minuteKey = new DateTime(curr.Timestamp.Year, curr.Timestamp.Month, curr.Timestamp.Day,
                    curr.Timestamp.Hour, curr.Timestamp.Minute, 0);
                if (lastActiveMinute != minuteKey)
                {
                    activeMinutes++;
                    lastActiveMinute = minuteKey;
                }
            }
        }

        return new ActivitySummaryDto(
            Math.Round(totalDistance, 1),
            activeMinutes,
            Math.Round(maxSpeed, 2),
            speedCount > 0 ? Math.Round(speedSum / speedCount, 2) : 0,
            points.Count,
            points.First().Timestamp,
            points.Last().Timestamp
        );
    }

    /// <summary>
    /// Piecewise linear LiPo discharge curve matching firmware/collar/src/power_manager.cpp.
    /// 4.20V=100%, 4.10V=90%, 3.95V=70%, 3.80V=40%, 3.70V=20%, 3.50V=5%, 3.20V=0%
    /// </summary>
    private static float VoltageToPercent(float voltage)
    {
        float mv = voltage * 1000f;
        if (mv >= 4200f) return 100f;
        if (mv <= 3200f) return 0f;

        float pct;
        if (mv > 4100f)      pct = 90f + (mv - 4100f) / (4200f - 4100f) * 10f;
        else if (mv > 3950f) pct = 70f + (mv - 3950f) / (4100f - 3950f) * 20f;
        else if (mv > 3800f) pct = 40f + (mv - 3800f) / (3950f - 3800f) * 30f;
        else if (mv > 3700f) pct = 20f + (mv - 3700f) / (3800f - 3700f) * 20f;
        else if (mv > 3500f) pct = 5f  + (mv - 3500f) / (3700f - 3500f) * 15f;
        else                 pct = (mv - 3200f) / (3500f - 3200f) * 5f;

        return Math.Clamp(pct, 0f, 100f);
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public async Task<FirmwareCheckDto> CheckFirmwareAsync(string collarId, string currentVersion)
    {
        var collar = await _db.CollarDevices.FirstOrDefaultAsync(c => c.CollarId == collarId);
        if (collar != null)
        {
            collar.FirmwareVersion = currentVersion;
            collar.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var latest = await _db.FirmwareReleases
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        if (latest == null || latest.Version == currentVersion)
            return new FirmwareCheckDto(false, null, null, null);

        return new FirmwareCheckDto(true, latest.Version, latest.FileSize, latest.ReleaseNotes);
    }

    public async Task<(Stream? Stream, string? ContentType, long? Length)?> DownloadFirmwareAsync(string collarId)
    {
        var latest = await _db.FirmwareReleases
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        if (latest == null || !File.Exists(latest.FilePath))
            return null;

        var stream = new FileStream(latest.FilePath, FileMode.Open, FileAccess.Read);
        return (stream, "application/octet-stream", latest.FileSize);
    }

    public async Task<FirmwareReleaseDto> UploadFirmwareAsync(
        int userId, string version, string? releaseNotes, Stream fileStream, string fileName)
    {
        var firmwareDir = Path.Combine("uploads", "firmware");
        Directory.CreateDirectory(firmwareDir);
        var filePath = Path.Combine(firmwareDir, $"collar_{version}.bin");

        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fs);
        }

        var fileInfo = new FileInfo(filePath);

        // Compute SHA-256
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var hashStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var hash = await sha256.ComputeHashAsync(hashStream);
        var hashHex = Convert.ToHexString(hash).ToLower();

        var release = new FirmwareRelease
        {
            Version = version,
            FilePath = filePath,
            FileSize = fileInfo.Length,
            Sha256Hash = hashHex,
            ReleaseNotes = releaseNotes,
            IsActive = true
        };

        _db.FirmwareReleases.Add(release);
        await _db.SaveChangesAsync();

        return new FirmwareReleaseDto(
            release.Id, release.Version, release.FileSize,
            release.Sha256Hash, release.ReleaseNotes, release.IsActive, release.CreatedAt);
    }

    public async Task<IEnumerable<FirmwareReleaseDto>> GetFirmwareReleasesAsync()
    {
        return await _db.FirmwareReleases
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FirmwareReleaseDto(
                f.Id, f.Version, f.FileSize,
                f.Sha256Hash, f.ReleaseNotes, f.IsActive, f.CreatedAt))
            .ToListAsync();
    }

    private static CollarDeviceDto ToDto(CollarDevice c)
    {
        return new CollarDeviceDto(
            c.Id, c.CollarId, c.Name, c.AnimalId, c.Animal?.Name,
            c.FirmwareVersion, c.BatteryPercent, c.BatteryVoltage,
            c.LastSeenAt, c.LastLatitude, c.LastLongitude, c.LastAccuracy,
            c.IsActive, c.CreatedAt
        );
    }
}
