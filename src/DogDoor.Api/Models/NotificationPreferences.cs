namespace DogDoor.Api.Models;

public class NotificationPreferences
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }

    public bool AnimalApproachInside { get; set; }
    public bool AnimalApproachOutside { get; set; }
    public bool UnknownAnimalInside { get; set; }
    public bool UnknownAnimalOutside { get; set; }
    public bool DoorOpened { get; set; }
    public bool DoorClosed { get; set; }
    public bool DoorFailedOpen { get; set; }
    public bool DoorFailedClose { get; set; }
    public bool PowerDisconnected { get; set; }
    public bool PowerRestored { get; set; }
    public bool BatteryLow { get; set; }
    public bool BatteryCharged { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
