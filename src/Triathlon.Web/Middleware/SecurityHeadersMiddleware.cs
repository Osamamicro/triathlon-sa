using Triathlon.Web.Areas.Public;

namespace Triathlon.Web.Middleware;

/// <summary>
/// Response headers every page should carry, everywhere — including the rendered error page after
/// an unhandled exception. The CSP is the strict one the public site can afford because nothing
/// there is inline any more (theme.js, timeline.js); the Blazor dashboard gets the same script rule
/// and a looser style rule for MudBlazor; Hangfire's dashboard is inline-script heavy and
/// admin-only, so it is exempt.
///
/// Headers are stamped from a <see cref="HttpResponse.OnStarting"/> callback rather than directly
/// in <see cref="InvokeAsync"/>. <c>UseExceptionHandler</c> calls <c>Response.Clear()</c> (which
/// clears <c>Response.Headers</c>) before re-executing to the error path, so anything written
/// up-front here would be wiped from every 500 response. <c>OnStarting</c> callbacks are held by
/// the server's response feature, not by the header collection, so they survive that clear and run
/// once, right before the final (possibly re-executed) response is sent.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string PublicCsp =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";

    private const string DashboardCsp =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; " +
        "font-src 'self' data:; connect-src 'self' ws: wss:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Captured now, before an exception handler might re-execute the request to /en/error or
        // /dashboard/error: the classification (public vs dashboard vs Hangfire) should reflect
        // where the visitor actually was, and re-execution lands in the same area anyway.
        var path = context.Request.Path;

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            if (path.StartsWithSegments("/dashboard/jobs", StringComparison.OrdinalIgnoreCase))
            {
                // Hangfire renders its own inline scripts and sends its own policy for them; it sits
                // behind the SuperAdmin filter already, so this middleware leaves that path alone.
            }
            else if (PublicSite.IsDashboardPath(path))
            {
                headers["Content-Security-Policy"] = DashboardCsp;
            }
            else
            {
                headers["Content-Security-Policy"] = PublicCsp;
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
