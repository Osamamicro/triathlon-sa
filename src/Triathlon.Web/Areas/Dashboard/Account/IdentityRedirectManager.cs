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

        // Same rules as ASP.NET Core's UrlHelper.IsLocalUrl, written out so the answer does not
        // depend on System.Uri's platform quirks: on Linux/macOS Uri.TryCreate("/dashboard",
        // UriKind.Absolute) succeeds because a leading slash is an implicit file path, which made
        // every genuine local path look absolute in CI.
        //
        // Local means: starts with exactly one "/" (a second "/" or "\" would be a network-path
        // reference the browser resolves to another host), and contains no control characters or
        // whitespace that could smuggle a scheme past the check.
        if (url[0] != '/')
        {
            return false;
        }

        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\'))
        {
            return false;
        }

        foreach (var c in url)
        {
            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return true;
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context) =>
        RedirectToWithStatus(navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path), message, context);
}
