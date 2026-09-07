using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Triathlon.Web.Services;

/// <summary>Sends mail through the Federation's SMTP relay with MailKit.</summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = options.Value;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        }.ToMessageBody();

        // A fresh connection per message: the volume here is a handful of transactional mails a day,
        // and a pooled connection to a relay that drops idle sessions costs more than it saves.
        using var client = new SmtpClient();

        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            ct);

        if (!string.IsNullOrEmpty(settings.User))
        {
            await client.AuthenticateAsync(settings.User, settings.Password, ct);
        }

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(quit: true, ct);

        logger.LogInformation("Sent \"{Subject}\" to {Recipient}.", message.Subject, message.To);
    }
}
