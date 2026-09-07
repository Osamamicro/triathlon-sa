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

        // SecureSocketOptions.Auto silently accepts a plaintext session if the relay does not offer
        // STARTTLS — fine for an anonymous relay, not for one an authenticated mailbox password is
        // about to go over. When STARTTLS is not explicitly requested, still ask for TLS: implicit
        // TLS on 465, otherwise upgrade-if-offered on 587/25, and refuse to authenticate below if
        // that upgrade did not actually happen.
        var secureSocketOptions = settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : settings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions, ct);

        if (!string.IsNullOrEmpty(settings.User))
        {
            if (!client.IsSecure)
            {
                throw new InvalidOperationException(
                    "Refusing to authenticate over an unencrypted SMTP connection.");
            }

            await client.AuthenticateAsync(settings.User, settings.Password, ct);
        }

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(quit: true, ct);

        logger.LogInformation("Sent \"{Subject}\" to {Recipient}.", message.Subject, message.To);
    }
}
