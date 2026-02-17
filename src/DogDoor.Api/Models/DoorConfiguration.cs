using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class DoorConfiguration
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool AutoCloseEnabled { get; set; } = true;

    public int AutoCloseDelaySeconds { get; set; } = 10;

    public double MinConfidenceThreshold { get; set; } = 0.7;

    public bool NightModeEnabled { get; set; }

    public TimeOnly? NightModeStart { get; set; }

    public TimeOnly? NightModeEnd { get; set; }

    [MaxLength(200)]
    public string? ApiKey { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
