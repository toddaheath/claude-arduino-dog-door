using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace DogDoor.Api.Services;

public class TwilioSmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IConfiguration config, ILogger<TwilioSmsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendSmsAsync(string toPhone, string message)
    {
        var accountSid = _config["Twilio:AccountSid"];
        var authToken = _config["Twilio:AuthToken"];
        var fromPhone = _config["Twilio:FromPhone"];

        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhone))
        {
            _logger.LogWarning("Twilio not configured; skipping SMS");
            return;
        }

        try
        {
            TwilioClient.Init(accountSid, authToken);
            await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromPhone),
                to: new PhoneNumber(toPhone));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio SMS failed for {Phone}", toPhone);
        }
    }
}
