namespace Triathlon.Web.Services;

/// <summary>
/// The Federation's Cloudflare Turnstile key pair. Both halves arrive per deployment — the site key
/// is public and goes into the widget's markup, the secret key never leaves the server and belongs
/// in an environment variable. Leaving either empty turns the challenge off, which is what a
/// developer machine and the test suite run with.
/// </summary>
public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    public string SiteKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>A half-configured pair is no configuration at all: both keys, or no challenge.</summary>
    public bool Enabled => SiteKey.Length > 0 && SecretKey.Length > 0;
}
