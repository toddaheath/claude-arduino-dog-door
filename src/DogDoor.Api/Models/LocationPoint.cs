using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class LocationPoint
{
    public long Id { get; set; }

    public int CollarDeviceId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public float? Altitude { get; set; }

    public float? Accuracy { get; set; }

    public float? Speed { get; set; }

    public float? Heading { get; set; }

    public int? Satellites { get; set; }

    public float? BatteryVoltage { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CollarDevice CollarDevice { get; set; } = null!;
}
