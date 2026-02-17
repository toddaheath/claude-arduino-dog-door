namespace DogDoor.Api.Models;

public class UserGuest
{
    public int OwnerId { get; set; }
    public int GuestId { get; set; }
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }

    public User Owner { get; set; } = null!;
    public User Guest { get; set; } = null!;
}
