using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    [MaxLength(8)]
    public string? TokenPrefix { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsUsed { get; set; }

    public User User { get; set; } = null!;
}
