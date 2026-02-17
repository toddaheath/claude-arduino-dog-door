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

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            // Not a standard email format; return a generic placeholder.
            return "[redacted-email]";
        }

        var localPart = email.Substring(0, atIndex);
        var domainPart = email.Substring(atIndex);

        if (localPart.Length <= 2)
        {
            // For very short local parts, just mask entirely.
            return "**" + domainPart;
        }

        var firstChar = localPart[0];
        var lastChar = localPart[localPart.Length - 1];
        var maskedMiddle = new string('*', localPart.Length - 2);

        return $"{firstChar}{maskedMiddle}{lastChar}{domainPart}";
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        var sanitizedEmail = toEmail?.Replace("\r", string.Empty).Replace("\n", string.Empty);
        // Sanitize email address for logging to prevent log forging via newline injection
        var safeEmailForLogging = toEmail?.Replace("\r", string.Empty).Replace("\n", string.Empty);
            _logger.LogWarning("SendGrid API key not configured; skipping email to {Email}", MaskEmail(toEmail));
            _logger.LogWarning("SendGrid API key not configured; skipping email to {Email}", sanitizedEmail);
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SendGrid API key not configured; skipping email to {Email}", safeEmailForLogging);
            return;
        }

        var fromEmail = _config["SendGrid:FromEmail"] ?? "noreply@dogdoor.app";
        var fromName = _config["SendGrid:FromName"] ?? "Smart Dog Door";

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
            _logger.LogError("SendGrid failed with status {Status} for {Email}", response.StatusCode, MaskEmail(toEmail));
            _logger.LogError("SendGrid failed with status {Status} for {Email}", response.StatusCode, sanitizedEmail);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SendGrid failed with status {Status} for {Email}", response.StatusCode, safeEmailForLogging);
        }
    }
}
