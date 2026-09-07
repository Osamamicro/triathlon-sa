using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Triathlon.Web.Services;

/// <summary>
/// Verifies a Turnstile token server-side. Cloudflare's widget only proves anything once its token
/// has been exchanged with siteverify from the server that received the form — the client-side
/// callback alone is worth nothing, because a script can post the field with any value it likes.
/// <para>
/// Fails closed: an empty token, a rejected token, a malformed answer, a timeout or an unreachable
/// endpoint all read as "not verified". A challenge that lets everything through while Cloudflare is
/// down is not a challenge, and the form it guards is a public write endpoint.
/// </para>
/// </summary>
public sealed class TurnstileVerifier(
    IHttpClientFactory httpClientFactory,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileVerifier> logger) : ITurnstileVerifier
{
    /// <summary>The named client, so its timeout and handler lifetime are configured in one place.</summary>
    public const string ClientName = "turnstile";

    public const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    /// <summary>Cloudflare answers in well under a second; a form post must not wait much longer.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["secret"] = options.Value.SecretKey,
            ["response"] = token,
        };

        // Optional in Cloudflare's API, and genuinely absent behind some proxies; sending an empty
        // value would be read as a claim about the client rather than as silence.
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            fields["remoteip"] = remoteIp;
        }

        try
        {
            using var content = new FormUrlEncodedContent(fields);
            var client = httpClientFactory.CreateClient(ClientName);
            using var response = await client.PostAsync(new Uri(VerifyUrl), content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile verification answered {StatusCode}; treating the submission as unverified.", (int)response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var document = JsonDocument.Parse(body);

            return document.RootElement.TryGetProperty("success", out var success)
                   && success.ValueKind == JsonValueKind.True;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Not ct's own cancellation: that surfaces as the request being abandoned anyway, and
            // failing closed on it costs one retry rather than admitting an unverified submission.
            logger.LogWarning(ex, "Turnstile verification failed to complete; treating the submission as unverified.");
            return false;
        }
    }
}
