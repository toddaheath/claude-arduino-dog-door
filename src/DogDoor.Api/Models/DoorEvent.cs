using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public enum DoorEventType
{
    AccessGranted,
    AccessDenied,
    DoorOpened,
    DoorClosed,
    UnknownAnimal,
    ManualOverride,
    ExitGranted,
    ExitDenied,
    EntryGranted,
    EntryDenied,
    AnimalApproach = 10,
    DoorObstructed = 11,
    PowerLost = 12,
    PowerRestored = 13,
    BatteryLow = 14,
    BatteryCharged = 15
}

public enum DoorSide
{
    Inside,
    Outside
}

public enum TransitDirection
{
    Entering,
    Exiting,
    Unknown
}

public class DoorEvent
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? AnimalId { get; set; }

    public DoorEventType EventType { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public double? ConfidenceScore { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public DoorSide? Side { get; set; }

    public TransitDirection? Direction { get; set; }

    public User? User { get; set; }
    public Animal? Animal { get; set; }
}
