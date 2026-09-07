using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Triathlon.Web.Areas.Dashboard.Account;
using Triathlon.Web.Domain.Identity;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// The one non-component endpoint the dashboard needs: signing out has to clear the cookie on the
/// server, which a Razor component cannot do once its response has started.
/// </summary>
internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var dashboard = endpoints.MapGroup("/dashboard");

        // The form-bound parameter is deliberate: it is what marks the endpoint as antiforgery-protected.
        dashboard.MapPost("/logout", async (
            [FromServices] SignInManager<AppUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();

            // The field comes off a posted form, so anyone can put anything in it, and LocalRedirect
            // throws on a non-local URL — which would turn a tampered sign-out into a 500 instead of
            // a sign-out. The same guard the login page uses decides, and the leading slash is
            // trimmed because "~//dashboard" is itself a network-path reference LocalRedirect rejects.
            return TypedResults.LocalRedirect(IdentityRedirectManager.IsLocalUrl(returnUrl)
                ? $"~/{returnUrl.TrimStart('/')}"
                : "~/dashboard/login");
        });

        // The only way to change the dashboard's language: it has no "culture" URL segment (see
        // PublicSite.AddPublicSite), so a cookie is the one signal RequestLocalization has for it.
        // AllowAnonymous because a visitor who cannot read English yet is exactly who needs this
        // before ever signing in.
        dashboard.MapGet("/culture/{culture:regex(^(en|ar)$)}", (
            HttpContext context,
            string culture,
            string? returnUrl) =>
        {
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = true,
                });

            return TypedResults.LocalRedirect(IdentityRedirectManager.IsLocalUrl(returnUrl)
                ? $"~/{returnUrl!.TrimStart('/')}"
                : "~/dashboard");
        }).AllowAnonymous();

        return dashboard;
    }
}
