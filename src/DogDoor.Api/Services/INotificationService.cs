using DogDoor.Api.Models;

namespace DogDoor.Api.Services;

public interface INotificationService
{
    Task NotifyAsync(int userId, DoorEventType eventType, string? animalName, DoorSide? side, string? notes = null);
}
