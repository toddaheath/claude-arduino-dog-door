using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class AnimalPhoto
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FileName { get; set; }

    [MaxLength(64)]
    public string? PHash { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Animal Animal { get; set; } = null!;
}
