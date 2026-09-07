using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Triathlon.Web.Infrastructure;

namespace Triathlon.Web.Middleware;

/// <summary>
/// Puts the staging site behind one shared HTTP Basic credential.
/// <para>
/// This is a keep-out sign for a work-in-progress site, not an authentication system — the real one
/// is Identity, under <c>/dashboard</c>. It runs first in the pipeline so nothing, including a
/// cached response, is served to an unauthenticated visitor; <c>/health</c> is the sole exception,
/// because the load balancer and the systemd unit have to be able to probe the site without
/// carrying a secret.
/// </para>
/// <para>
/// Once the credential has been checked the <c>Authorization</c> header is stripped from the
/// request, so the rest of the pipeline sees exactly what it would see in production. That is not
/// tidiness: browsers resend Basic credentials on every request, and the output cache refuses to
/// store or replay a response to a request carrying an <c>Authorization</c> header, so leaving it in
/// place would quietly disable caching on staging — the one environment where the caching is meant
/// to be rehearsed. Nothing downstream reads it; Identity's own sign-in is a cookie.
/// </para>
/// </summary>
public sealed class StagingBasicAuthMiddleware
{
    /// <summary>The one path that answers without the credential.</summary>
    public const string HealthPath = "/health";

    private const string Scheme = "Basic";
    private const string Realm = "staging";

    private readonly RequestDelegate _next;

    /// <summary>
    /// The expected "user:password" hashed once at startup. Comparing fixed-length hashes rather
    /// than the raw strings keeps the check constant-time in the credential's length as well as its
    /// content, so a wrong guess reveals nothing about either.
    /// </summary>
    private readonly byte[] _expected;

    public StagingBasicAuthMiddleware(RequestDelegate next, IOptions<SiteOptions> options)
    {
        _next = next;

        var credentials = options.Value.BasicAuth;
        _expected = Hash($"{credentials.User}:{credentials.Password}");
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            return _next(context);
        }

        if (!IsAuthorized(context.Request))
        {
            return ChallengeAsync(context);
        }

        // Checked and done with. See the class remarks: carrying it further turns off output caching.
        context.Request.Headers.Remove(HeaderNames.Authorization);

        return _next(context);
    }

    private bool IsAuthorized(HttpRequest request)
    {
        if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var header) ||
            !Scheme.Equals(header.Scheme, StringComparison.OrdinalIgnoreCase) ||
            header.Parameter is null)
        {
            return false;
        }

        Span<byte> decoded = stackalloc byte[256];
        if (!Convert.TryFromBase64String(header.Parameter, decoded, out var length))
        {
            return false;
        }

        var offered = Hash(Encoding.UTF8.GetString(decoded[..length]));

        return CryptographicOperations.FixedTimeEquals(offered, _expected);
    }

    private static Task ChallengeAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers[HeaderNames.WWWAuthenticate] = $"{Scheme} realm=\"{Realm}\"";
        context.Response.ContentType = "text/plain; charset=utf-8";

        return context.Response.WriteAsync("This staging site is not public.");
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}

/// <summary>Registers the staging gate, and only on a staging deployment.</summary>
public static class StagingBasicAuthExtensions
{
    public static WebApplication UseStagingBasicAuth(this WebApplication app)
    {
        if (!app.Configuration.GetValue($"{SiteOptions.SectionName}:Staging", false))
        {
            return app;
        }

        var user = app.Configuration[$"{SiteOptions.SectionName}:BasicAuth:User"];
        var password = app.Configuration[$"{SiteOptions.SectionName}:BasicAuth:Password"];

        // Failing to boot is the only safe response: a staging site that came up without its gate
        // would be a public, unfinished copy of the Federation's website.
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Site:Staging is enabled but Site:BasicAuth:User and Site:BasicAuth:Password are not both set.");
        }

        app.UseMiddleware<StagingBasicAuthMiddleware>();

        return app;
    }
}
