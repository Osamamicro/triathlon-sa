using Hangfire;
using Triathlon.Web.Services;

namespace Triathlon.Web.Jobs;

/// <summary>
/// Sends one queued mail through Hangfire rather than inline in a request, so a slow or unreachable
/// SMTP server never becomes a slow page — and Hangfire retries a failed send on its own schedule
/// instead of the caller catching the exception and losing the attempt.
/// </summary>
public sealed class EmailJob(IEmailSender sender)
{
    [AutomaticRetry(Attempts = 5)]
    public Task SendAsync(EmailMessage message, CancellationToken ct) => sender.SendAsync(message, ct);
}
