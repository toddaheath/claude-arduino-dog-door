using DogDoor.Api.Data;
using DogDoor.Api.DTOs;
using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Services;

public class GeofenceService : IGeofenceService
{
    private readonly DogDoorDbContext _db;

    public GeofenceService(DogDoorDbContext db)
    {
        _db = db;
    }

    public async Task<GeofenceDto> CreateGeofenceAsync(int userId, CreateGeofenceDto dto)
    {
        var fence = new Geofence
        {
            UserId = userId,
            Name = dto.Name,
            FenceType = dto.FenceType,
            Rule = dto.Rule,
            BoundaryJson = dto.BoundaryJson,
            BuzzerPattern = dto.BuzzerPattern,
            IsActive = true,
            Version = 1
        };

        _db.Geofences.Add(fence);
        await _db.SaveChangesAsync();

        return ToDto(fence);
    }

    public async Task<IEnumerable<GeofenceDto>> GetGeofencesAsync(int userId)
    {
        return await _db.Geofences
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Name)
            .Select(f => ToDto(f))
            .ToListAsync();
    }

    public async Task<GeofenceDto?> GetGeofenceAsync(int userId, int geofenceId)
    {
        var fence = await _db.Geofences
            .FirstOrDefaultAsync(f => f.Id == geofenceId && f.UserId == userId);

        return fence == null ? null : ToDto(fence);
    }

    public async Task<GeofenceDto?> UpdateGeofenceAsync(int userId, int geofenceId, UpdateGeofenceDto dto)
    {
        var fence = await _db.Geofences
            .FirstOrDefaultAsync(f => f.Id == geofenceId && f.UserId == userId);

        if (fence == null) return null;

        if (dto.Name != null) fence.Name = dto.Name;
        if (dto.Rule != null) fence.Rule = dto.Rule;
        if (dto.BoundaryJson != null) fence.BoundaryJson = dto.BoundaryJson;
        if (dto.BuzzerPattern != null) fence.BuzzerPattern = dto.BuzzerPattern.Value;
        if (dto.IsActive != null) fence.IsActive = dto.IsActive.Value;

        fence.Version++;
        fence.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(fence);
    }

    public async Task<bool> DeleteGeofenceAsync(int userId, int geofenceId)
    {
        var fence = await _db.Geofences
            .FirstOrDefaultAsync(f => f.Id == geofenceId && f.UserId == userId);

        if (fence == null) return false;

        _db.Geofences.Remove(fence);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<GeofenceSyncDto> GetGeofenceSyncAsync(int userId, int sinceVersion)
    {
        var fences = await _db.Geofences
            .Where(f => f.UserId == userId && f.IsActive && f.Version > sinceVersion)
            .OrderBy(f => f.Id)
            .ToListAsync();

        var maxVersion = fences.Any()
            ? fences.Max(f => f.Version)
            : sinceVersion;

        return new GeofenceSyncDto(
            maxVersion,
            fences.Select(f => ToDto(f)).ToArray()
        );
    }

    public async Task<int> RecordGeofenceEventsAsync(string collarId, GeofenceEventInputDto[] events)
    {
        var collar = await _db.CollarDevices
            .FirstOrDefaultAsync(c => c.CollarId == collarId);

        if (collar == null) return 0;

        var geofenceEvents = events.Select(e => new GeofenceEvent
        {
            GeofenceId = e.FenceId,
            CollarDeviceId = collar.Id,
            EventType = e.Type,
            Latitude = e.Lat,
            Longitude = e.Lng,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(e.Ts).UtcDateTime
        }).ToList();

        _db.GeofenceEvents.AddRange(geofenceEvents);
        await _db.SaveChangesAsync();
        return geofenceEvents.Count;
    }

    public async Task<IEnumerable<GeofenceEventDto>> GetGeofenceEventsAsync(
        int userId, int? geofenceId, DateTime? from, DateTime? to)
    {
        var query = _db.GeofenceEvents
            .Include(e => e.Geofence)
            .Include(e => e.CollarDevice)
            .Where(e => e.Geofence.UserId == userId);

        if (geofenceId.HasValue)
            query = query.Where(e => e.GeofenceId == geofenceId.Value);
        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Take(100)
            .Select(e => new GeofenceEventDto(
                e.Id, e.GeofenceId, e.Geofence.Name,
                e.CollarDeviceId, e.CollarDevice.Name,
                e.EventType, e.Latitude, e.Longitude, e.Timestamp))
            .ToListAsync();
    }

    private static GeofenceDto ToDto(Geofence f)
    {
        return new GeofenceDto(
            f.Id, f.Name, f.FenceType, f.Rule, f.BoundaryJson,
            f.BuzzerPattern, f.IsActive, f.Version,
            f.CreatedAt, f.UpdatedAt
        );
    }
}
