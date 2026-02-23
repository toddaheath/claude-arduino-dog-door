namespace DogDoor.Api.Services;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetToken);
    Task SendGuestInvitationAsync(string toEmail, string invitedByName, string acceptToken);
    Task SendUsernameLookupAsync(string toEmail, string email);
    Task SendNotificationAsync(string toEmail, string subject, string body);
}
