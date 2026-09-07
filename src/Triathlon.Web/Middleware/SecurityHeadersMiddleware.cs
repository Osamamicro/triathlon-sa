using Triathlon.Web.Areas.Public;

namespace Triathlon.Web.Middleware;

/// <summary>
/// Response headers every page should carry. The CSP is the strict one the public site can afford
/// because nothing there is inline any more (theme.js, timeline.js); the Blazor dashboard gets the
/// same script rule and a looser style rule for MudBlazor; Hangfire's dashboard is inline-script
/// heavy and admin-only, so it is exempt.
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

        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        var path = context.Request.Path;
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

        return next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
