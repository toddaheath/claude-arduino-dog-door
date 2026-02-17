using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class Animal
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Breed { get; set; }

    public bool IsAllowed { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<AnimalPhoto> Photos { get; set; } = new List<AnimalPhoto>();
    public ICollection<DoorEvent> DoorEvents { get; set; } = new List<DoorEvent>();
}
