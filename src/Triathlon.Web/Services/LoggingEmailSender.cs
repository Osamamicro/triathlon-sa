namespace Triathlon.Web.Services;

/// <summary>
/// Stands in for the SMTP sender when no host is configured, so a developer can run the whole
/// application — registrations, password resets — without a mail server or a set of credentials.
/// Only the recipient and the subject are logged: the body of a transactional mail can contain a
/// reset token or an athlete's personal details, and neither belongs in a log file.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        logger.LogInformation(
            "Email not sent (no SMTP host configured): \"{Subject}\" to {Recipient}.",
            message.Subject,
            message.To);

        return Task.CompletedTask;
    }
}
