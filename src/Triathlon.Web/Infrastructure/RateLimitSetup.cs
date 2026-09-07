using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace Triathlon.Web.Infrastructure;

/// <summary>
/// Rate limiting for the public website's write endpoints — the athlete registration form, the
/// contact form, and anything else that lets an anonymous visitor cause work.
/// <para>
/// Partitioned by client IP, so one abusive source cannot lock everyone else out. Behind a reverse
/// proxy that address is only meaningful once forwarded headers have been applied, which is why
/// <c>UseForwardedHeaders</c> runs first in the pipeline.
/// </para>
/// </summary>
public static class RateLimitSetup
{
    /// <summary>Policy name for anonymous public form posts.</summary>
    public const string PublicPost = "public-post";

    private const int PermitLimit = 10;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAppRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, ct) =>
            {
                var response = context.HttpContext.Response;

                // Tell the caller when to come back. A fixed window without queueing does not always
                // publish the metadata, so the window length is the fallback: never worse than the
                // real answer, and it keeps well-behaved clients from hammering the endpoint.
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata)
                    ? metadata
                    : Window;

                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Headers[HeaderNames.RetryAfter] =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                // The body is written here, rather than left empty, for a reason beyond politeness:
                // UseStatusCodePagesWithReExecute re-runs any bodiless 4xx through the not-found
                // page, and re-running a POST that way lands on the antiforgery check and turns this
                // 429 into a 400. Setting a content type and a body makes that middleware stand down.
                response.ContentType = "text/plain; charset=utf-8";

                return new ValueTask(response.WriteAsync(
                    "Too many requests from this address. Please wait a minute and try again.", ct));
            };

            options.AddPolicy(PublicPost, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    Window = Window,
                    // Reject immediately rather than parking requests: a form post that is going to
                    // be refused should be refused now, not after holding a connection open.
                    QueueLimit = 0,
                }));
        });

        return services;
    }
}
