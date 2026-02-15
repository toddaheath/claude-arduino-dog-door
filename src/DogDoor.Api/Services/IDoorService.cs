using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IDoorService
{
    Task<AccessResponseDto> ProcessAccessRequestAsync(Stream imageStream, string? apiKey, string? side = null);
    Task<DoorConfigurationDto> GetConfigurationAsync();
    Task<DoorConfigurationDto> UpdateConfigurationAsync(UpdateDoorConfigurationDto dto);
    Task<IEnumerable<DoorEventDto>> GetAccessLogsAsync(int page, int pageSize, string? eventType, string? direction = null);
    Task<DoorEventDto?> GetAccessLogAsync(int id);
}
