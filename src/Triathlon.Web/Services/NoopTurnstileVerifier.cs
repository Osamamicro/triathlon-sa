namespace Triathlon.Web.Services;

/// <summary>
/// The verifier registered when no Turnstile keys are configured: there is no widget on the form,
/// so there is no token to check and every submission passes. Development and the integration tests
/// run on this one, which is why they can post a form without a browser.
/// </summary>
public sealed class NoopTurnstileVerifier : ITurnstileVerifier
{
    public Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct) => Task.FromResult(true);
}
