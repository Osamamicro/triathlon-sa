using System.Text.RegularExpressions;

namespace Triathlon.Web.Domain.Common;

/// <summary>
/// The one slug rule for every slugged aggregate (events, pages, news, guides, rules): lower-case
/// ASCII words joined by single hyphens, at most 128 characters. No dots — a dot makes
/// <c>PublicSite.WantsHtmlStatusPage</c> treat the URL as a static file.
/// </summary>
public static partial class Slugs
{
    public const int MaxLength = 128;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public static bool IsValid(string? slug) =>
        !string.IsNullOrEmpty(slug) && slug.Length <= MaxLength && Pattern().IsMatch(slug);
}
