using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; }

    public User User { get; set; } = null!;
}
