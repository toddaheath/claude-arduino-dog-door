using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IGeofenceService
{
    Task<GeofenceDto> CreateGeofenceAsync(int userId, CreateGeofenceDto dto);
    Task<IEnumerable<GeofenceDto>> GetGeofencesAsync(int userId);
    Task<GeofenceDto?> GetGeofenceAsync(int userId, int geofenceId);
    Task<GeofenceDto?> UpdateGeofenceAsync(int userId, int geofenceId, UpdateGeofenceDto dto);
    Task<bool> DeleteGeofenceAsync(int userId, int geofenceId);
    Task<GeofenceSyncDto> GetGeofenceSyncAsync(int userId, int sinceVersion);
    Task<int> RecordGeofenceEventsAsync(string collarId, GeofenceEventInputDto[] events);
    Task<IEnumerable<GeofenceEventDto>> GetGeofenceEventsAsync(int userId, int? geofenceId, DateTime? from, DateTime? to);
}
