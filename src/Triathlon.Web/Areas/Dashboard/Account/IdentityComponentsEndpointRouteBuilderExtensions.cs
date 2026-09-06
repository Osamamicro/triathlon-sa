using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        return dashboard;
    }
}
