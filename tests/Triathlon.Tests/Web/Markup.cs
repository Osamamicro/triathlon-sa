using System.Text.RegularExpressions;

namespace Triathlon.Tests.Web;

/// <summary>
/// Shared markup assertions for the public site's rendered HTML.
/// </summary>
public static partial class Markup
{
    [GeneratedRegex(@"<script\b([^>]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTag();

    [GeneratedRegex(@"\bsrc\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex HasSrcAttribute();

    [GeneratedRegex("""\btype\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase)]
    private static partial Regex TypeAttribute();

    /// <summary>
    /// Whether <paramref name="html"/> contains a <c>&lt;script&gt;</c> tag that a strict
    /// <c>script-src 'self'</c> CSP would block: one with no <c>src</c> (so its body would have to
    /// run inline) and whose <c>type</c> is not <c>application/ld+json</c>. JSON-LD is data a
    /// browser never executes as script regardless of CSP, so a page carrying structured data in a
    /// <c>&lt;script type="application/ld+json"&gt;</c> element still passes.
    /// </summary>
    public static bool HasInlineScript(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        foreach (Match tag in ScriptTag().Matches(html))
        {
            var attributes = tag.Groups[1].Value;

            if (HasSrcAttribute().IsMatch(attributes))
            {
                continue;
            }

            var type = TypeAttribute().Match(attributes);
            if (type.Success && string.Equals(type.Groups[1].Value.Trim(), "application/ld+json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
