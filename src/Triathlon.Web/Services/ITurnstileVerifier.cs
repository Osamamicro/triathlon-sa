namespace Triathlon.Web.Services;

/// <summary>
/// Checks the token a Turnstile widget put in the form against Cloudflare's siteverify endpoint.
/// Implementations decide nothing about the form itself — they answer "is this token good", and the
/// endpoint decides what to do about it.
/// </summary>
public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct);
}
