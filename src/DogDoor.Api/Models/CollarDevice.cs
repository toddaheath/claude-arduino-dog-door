using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class CollarDevice
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? AnimalId { get; set; }

    [Required]
    [MaxLength(32)]
    public string CollarId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 shared secret for NFC authentication (base64-encoded, 32 bytes).
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string SharedSecret { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? FirmwareVersion { get; set; }

    public float? BatteryPercent { get; set; }

    public float? BatteryVoltage { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public double? LastLatitude { get; set; }

    public double? LastLongitude { get; set; }

    public float? LastAccuracy { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Animal? Animal { get; set; }
    public ICollection<LocationPoint> LocationPoints { get; set; } = new List<LocationPoint>();
    public ICollection<GeofenceEvent> GeofenceEvents { get; set; } = new List<GeofenceEvent>();
}
