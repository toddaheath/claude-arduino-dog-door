using SendGrid;
using SendGrid.Helpers.Mail;

namespace DogDoor.Api.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetToken)
    {
        var subject = "Reset your Smart Dog Door password";
        var body = $"<p>You requested a password reset. Use this token to reset your password:</p>" +
                   $"<p><strong>{resetToken}</strong></p>" +
                   $"<p>This token expires in 1 hour. If you did not request this, ignore this email.</p>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendGuestInvitationAsync(string toEmail, string invitedByName, string acceptToken)
    {
        var subject = $"{invitedByName} invited you to Smart Dog Door";
        var body = $"<p>{invitedByName} has invited you to view their Smart Dog Door data.</p>" +
                   $"<p>Use this token to accept: <strong>{acceptToken}</strong></p>" +
                   $"<p>This invitation expires in 7 days.</p>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendUsernameLookupAsync(string toEmail, string email)
    {
        var subject = "Your Smart Dog Door account";
        var body = $"<p>Your account email address is: <strong>{email}</strong></p>" +
                   $"<p>If you did not request this, ignore this email.</p>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendNotificationAsync(string toEmail, string subject, string body)
    {
        await SendEmailAsync(toEmail, subject, body);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return string.Empty;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "[redacted-email]";

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..];

        if (localPart.Length <= 2)
            return $"{new string('*', localPart.Length)}{domainPart}";

        return $"{localPart[0]}{new string('*', localPart.Length - 2)}{localPart[^1]}{domainPart}";
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SendGrid API key not configured; skipping email to {Email}", MaskEmail(toEmail));
            return;
        }

        var fromEmail = _config["SendGrid:FromEmail"] ?? "noreply@dogdoor.app";
        var fromName = _config["SendGrid:FromName"] ?? "Smart Dog Door";

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SendGrid failed with status {Status} for {Email}", response.StatusCode, MaskEmail(toEmail));
        }
    }
}
