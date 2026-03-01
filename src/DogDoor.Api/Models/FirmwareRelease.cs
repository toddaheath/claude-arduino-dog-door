using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class FirmwareRelease
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(64)]
    public string? Sha256Hash { get; set; }

    [MaxLength(500)]
    public string? ReleaseNotes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
