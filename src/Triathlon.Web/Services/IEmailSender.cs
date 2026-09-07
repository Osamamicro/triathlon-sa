namespace Triathlon.Web.Services;

/// <summary>
/// One outgoing message. HTML is the body the Federation writes; the plain-text alternative is
/// optional but worth supplying, because some corporate mail clients still prefer it.
/// </summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string? TextBody = null);

/// <summary>
/// Sends transactional mail: registration confirmations, password resets, event notices.
/// <para>
/// Implementations are expected to be called from a background job rather than inline in a request,
/// so a slow or unreachable SMTP server never becomes a slow page.
/// </para>
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
