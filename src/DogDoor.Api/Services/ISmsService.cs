namespace DogDoor.Api.Services;

public interface ISmsService
{
    Task SendSmsAsync(string toPhone, string message);
}
