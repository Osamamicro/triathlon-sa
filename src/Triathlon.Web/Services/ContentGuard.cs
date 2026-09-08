using Ganss.Xss;
using Microsoft.Extensions.Options;

namespace Triathlon.Web.Services;

/// <summary>
/// The only route editor input takes into storage. HTML is reduced to the allow-list below; file
/// paths must be site-relative under <c>/docs</c> or the configured media prefix. Views keep
/// rendering these fields with <c>Html.Raw</c>, so this class is the whole XSS boundary.
/// </summary>
public sealed class ContentGuard
{
    private const string DocsPrefix = "/docs";

    private static readonly string[] AllowedTags =
    [
        "p", "br", "strong", "em", "b", "i", "u", "s", "ul", "ol", "li", "a", "h2", "h3", "h4",
        "blockquote", "table", "thead", "tbody", "tr", "th", "td", "span", "div", "sup", "sub",
    ];

    private static readonly string[] AllowedAttributes = ["href", "title", "target", "rel", "class"];

    private static readonly string[] AllowedSchemes = ["http", "https", "mailto", "tel"];

    /// <summary>
    /// Tags whose text content must never survive removal. <see cref="HtmlSanitizer.KeepChildNodes"/>
    /// is on so an unwrapped <c>&lt;font&gt;</c> or similar keeps its visible text, but the same
    /// behaviour would otherwise let a stripped <c>&lt;script&gt;</c>/<c>&lt;style&gt;</c> tag's raw
    /// source leak into the page as plain text — the tag is gone, its payload is not.
    /// </summary>
    private static readonly string[] ContentRemovedTags = ["script", "style"];

    private readonly HtmlSanitizer _sanitizer;
    private readonly string _mediaPrefix;

    public ContentGuard(IOptions<MediaOptions> media)
    {
        ArgumentNullException.ThrowIfNull(media);
        _mediaPrefix = media.Value.NormalizedPublicPrefix;

        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.UnionWith(AllowedTags);
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.UnionWith(AllowedAttributes);
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.UnionWith(AllowedSchemes);
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedAtRules.Clear();
        _sanitizer.AllowDataAttributes = false;
        _sanitizer.KeepChildNodes = true;
        _sanitizer.RemovingTag += (_, e) =>
        {
            if (e.Reason == RemoveReason.NotAllowedTag && ContentRemovedTags.Contains(e.Tag.TagName, StringComparer.OrdinalIgnoreCase))
            {
                e.Tag.TextContent = "";
            }
        };
    }

    /// <summary>Editor HTML reduced to the allow-list; null when there is nothing left worth storing.</summary>
    public string? Html(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var clean = _sanitizer.Sanitize(html).Trim();
        return clean.Length == 0 ? null : clean;
    }

    /// <summary>The path unchanged when it is a site file, null when blank; otherwise a validation error.</summary>
    public string? FilePath(string? path, string field = "FilePath")
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return IsSiteFilePath(path)
            ? path
            : throw new ContentValidationException(field, "Validation_FilePath", path, DocsPrefix, _mediaPrefix);
    }

    public bool IsSiteFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path[0] != '/' || path.Contains("//", StringComparison.Ordinal)
            || path.Contains('?', StringComparison.Ordinal) || path.Contains('#', StringComparison.Ordinal)
            || path.Contains('\\', StringComparison.Ordinal) || path.Contains(':', StringComparison.Ordinal)
            || path.Contains('%', StringComparison.Ordinal) || path.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
        {
            return false;
        }

        var underDocs = path.StartsWith(DocsPrefix + "/", StringComparison.Ordinal);
        var underMedia = path.StartsWith(_mediaPrefix + "/", StringComparison.Ordinal);
        if (!underDocs && !underMedia)
        {
            return false;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // At least prefix + file name, and no dot-segments anywhere.
        return segments.Length >= 2 && segments.All(s => s != "." && s != "..") && !path.EndsWith('/');
    }
}
