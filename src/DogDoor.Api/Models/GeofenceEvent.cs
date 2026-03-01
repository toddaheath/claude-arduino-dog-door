using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class GeofenceEvent
{
    public long Id { get; set; }

    public int GeofenceId { get; set; }

    public int CollarDeviceId { get; set; }

    /// <summary>
    /// "entered", "exited", or "breach"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Geofence Geofence { get; set; } = null!;
    public CollarDevice CollarDevice { get; set; } = null!;
}
