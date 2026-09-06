using Microsoft.AspNetCore.Components;

namespace Triathlon.Web.Areas.Dashboard.Account;

/// <summary>
/// Navigation helper for the statically rendered account pages. A redirect there is a real HTTP
/// redirect, so a success or failure message has to survive it — hence the short-lived status cookie.
/// </summary>
internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    /// <summary>Navigates to <paramref name="uri"/>, forcing anything absolute back onto this host.</summary>
    public void RedirectTo(string? uri)
    {
        uri ??= string.Empty;

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context) =>
        RedirectToWithStatus(navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path), message, context);
}
