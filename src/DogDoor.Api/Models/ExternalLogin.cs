namespace DogDoor.Api.Models;

public class ExternalLogin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ExternalLoginProvider Provider { get; set; }
    public string ProviderUserId { get; set; } = string.Empty;
    public string? ProviderEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
