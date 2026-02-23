using DogDoor.Api.DTOs;

namespace DogDoor.Api.Services;

public interface INotificationPreferencesService
{
    Task<NotificationPreferencesDto> GetAsync(int userId);
    Task<NotificationPreferencesDto> UpdateAsync(int userId, UpdateNotificationPreferencesDto dto);
}
