using System.ComponentModel.DataAnnotations;

namespace DogDoor.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(30)]
    public string? MobilePhone { get; set; }

    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Animal> Animals { get; set; } = new List<Animal>();
    public ICollection<DoorConfiguration> DoorConfigurations { get; set; } = new List<DoorConfiguration>();
    public ICollection<UserGuest> OwnedGuests { get; set; } = new List<UserGuest>();
    public ICollection<UserGuest> GuestOf { get; set; } = new List<UserGuest>();
    public ICollection<Invitation> SentInvitations { get; set; } = new List<Invitation>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
