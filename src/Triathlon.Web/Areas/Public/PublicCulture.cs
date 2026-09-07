using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Triathlon.Web.Areas.Public;

/// <summary>
/// The culture-aware URL helpers the public views need: which culture the current request is in,
/// what the other one is, and what today's URL looks like under it.
/// </summary>
public static class PublicCulture
{
    /// <summary>
    /// The culture segment of the request being rendered, normalised to lower case. The route
    /// constraint is case-insensitive, so "/EN" reaches this page too and must still emit
    /// <c>lang="en"</c> — CSS attribute selectors match <c>lang</c> case-sensitively.
    /// </summary>
    public static string Of(ViewContext context)
    {
        var culture = context.RouteData.Values["culture"] as string;
        return string.Equals(culture, "ar", StringComparison.OrdinalIgnoreCase)
            ? "ar"
            : PublicSite.DefaultCulture;
    }

    /// <summary>The culture the language toggle switches to.</summary>
    public static string Other(string culture) => culture == "ar" ? "en" : "ar";

    /// <summary>A path inside the given culture, e.g. <c>Url("ar", "events")</c> → <c>/ar/events</c>.</summary>
    public static string Url(string culture, string relativePath = "") =>
        relativePath.Length == 0 ? "/" + culture : $"/{culture}/{relativePath}";

    /// <summary>
    /// The current URL with its culture segment swapped, query string intact — so switching language
    /// keeps the visitor on the page they were reading instead of dropping them on the home page.
    /// </summary>
    public static string SwitchUrl(ViewContext context)
    {
        var request = context.HttpContext.Request;
        var segments = (request.Path.Value ?? "/").Split('/');

        // segments[0] is the empty string before the leading slash; segments[1] is the culture.
        if (segments.Length > 1)
        {
            segments[1] = Other(Of(context));
        }

        return string.Join('/', segments) + request.QueryString;
    }

    /// <summary>
    /// The current request's path (query string dropped — <see cref="Microsoft.AspNetCore.Http.HttpRequest.Path"/>
    /// never carries one) with its culture segment swapped to <paramref name="targetCulture"/>. This is
    /// what a canonical or hreflang link needs; <see cref="SwitchUrl"/> keeps the query string instead,
    /// which is right for the visible language toggle but wrong for a link search engines compare
    /// byte-for-byte across cultures.
    /// </summary>
    public static string PathForCulture(HttpContext context, string targetCulture)
    {
        ArgumentNullException.ThrowIfNull(context);

        var segments = (context.Request.Path.Value ?? "/").Split('/');
        if (segments.Length > 1)
        {
            segments[1] = targetCulture;
        }

        return string.Join('/', segments);
    }
}
