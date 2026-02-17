using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class Invitation
{
    public int Id { get; set; }

    public int InvitedById { get; set; }

    [Required]
    [MaxLength(256)]
    public string InviteeEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User InvitedBy { get; set; } = null!;
}
