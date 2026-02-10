using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface IDoorService
{
    Task<AccessResponseDto> ProcessAccessRequestAsync(Stream imageStream, string? apiKey);
    Task<DoorConfigurationDto> GetConfigurationAsync();
    Task<DoorConfigurationDto> UpdateConfigurationAsync(UpdateDoorConfigurationDto dto);
    Task<IEnumerable<DoorEventDto>> GetAccessLogsAsync(int page, int pageSize, string? eventType);
    Task<DoorEventDto?> GetAccessLogAsync(int id);
}
