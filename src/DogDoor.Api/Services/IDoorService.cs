using DogDoor.Api.DTOs;
using DogDoor.Api.Models;

namespace DogDoor.Api.Services;

public interface IDoorService
{
    Task<AccessResponseDto> ProcessAccessRequestAsync(Stream imageStream, string? apiKey, string? side = null);
    Task<DoorConfigurationDto> GetConfigurationAsync(int userId);
    Task<DoorConfigurationDto> UpdateConfigurationAsync(UpdateDoorConfigurationDto dto, int userId);
    Task<IEnumerable<DoorEventDto>> GetAccessLogsAsync(int page, int pageSize, string? eventType, string? direction, int userId);
    Task<DoorEventDto?> GetAccessLogAsync(int id, int userId);
    Task RecordFirmwareEventAsync(string? apiKey, DoorEventType eventType, string? notes, double? batteryVoltage);
    Task RecordApproachPhotoAsync(Stream imageStream, string? apiKey, string? side);
}
