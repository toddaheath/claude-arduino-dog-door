using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class Geofence
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// "polygon", "circle", or "corridor"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string FenceType { get; set; } = "polygon";

    /// <summary>
    /// "allow" or "deny"
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Rule { get; set; } = "allow";

    /// <summary>
    /// GeoJSON geometry stored as JSON string.
    /// </summary>
    [Required]
    public string BoundaryJson { get; set; } = "{}";

    /// <summary>
    /// Buzzer pattern: 0=silent, 1=short, 2=long, 3=continuous
    /// </summary>
    public int BuzzerPattern { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Monotonically increasing version for sync protocol.
    /// </summary>
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<GeofenceEvent> GeofenceEvents { get; set; } = new List<GeofenceEvent>();
}
