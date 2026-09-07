using Microsoft.AspNetCore.Components;

namespace Triathlon.Web.Areas.Dashboard.Account;

/// <summary>
/// Navigation helper for the statically rendered account pages. A redirect there is a real HTTP
/// redirect, so a success or failure message has to survive it — hence the short-lived status cookie.
/// </summary>
internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    /// <summary>Where a redirect lands when the requested destination is missing or unsafe.</summary>
    private const string DefaultReturnUrl = "dashboard";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    /// <summary>Navigates to <paramref name="uri"/>, forcing anything unsafe back onto this host.</summary>
    public void RedirectTo(string? uri)
    {
        uri ??= string.Empty;

        if (IsLocalUrl(uri))
        {
            navigationManager.NavigateTo(uri);
            return;
        }

        // Not a safe relative URL. The one legitimate remaining case is an absolute URL built
        // from NavigationManager itself (see RedirectToCurrentPageWithStatus below) that happens
        // to be on this exact origin — bring that back to a relative path. Anything else (a
        // foreign host, a protocol-relative "//evil.com", a "javascript:" scheme, ...) falls back
        // to a known-safe page instead of ever being handed to NavigateTo.
        if (Uri.TryCreate(uri, UriKind.Absolute, out var absolute) &&
            Uri.TryCreate(navigationManager.BaseUri, UriKind.Absolute, out var baseUri) &&
            Uri.Compare(absolute, baseUri, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0)
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }
        else
        {
            uri = DefaultReturnUrl;
        }

        navigationManager.NavigateTo(uri);
    }

    /// <summary>
    /// True only for a same-origin, relative URL — never a scheme (<c>https://…</c>,
    /// <c>javascript:…</c>) and never a network-path reference (<c>//evil.com</c>, <c>/\evil.com</c>)
    /// that browsers and <see cref="NavigationManager.NavigateTo(string, bool)"/> resolve off-host.
    /// </summary>
    public static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        url = url.Trim();

        // Rejects absolute URIs, including scheme-based redirects like "javascript:alert(1)".
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return false;
        }

        // Rejects network-path references: a browser/NavigateTo treats a leading "//" or "/\" as
        // "same scheme, different host" even though Uri.IsWellFormedUriString calls it relative.
        if (url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        return Uri.IsWellFormedUriString(url, UriKind.Relative);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context) =>
        RedirectToWithStatus(navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path), message, context);
}
