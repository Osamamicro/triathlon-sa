using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// <see cref="ContentService.SafeHref"/> is the whole gate between an editor-typed link and
/// storage: a site path, a fragment, mailto/tel, or an absolute http(s) URL survive; anything that
/// would leave the site under a disguise (a scheme-relative "//host" or a backslash a browser
/// normalises to one) is refused.
/// </summary>
public sealed class SafeHrefTests
{
    [Theory]
    [InlineData("events", "events")]
    [InlineData("/join#clubs", "join#clubs")]
    [InlineData("#clubs", "#clubs")]
    [InlineData("mailto:a@b.c", "mailto:a@b.c")]
    [InlineData("tel:+966", "tel:+966")]
    [InlineData("https://x.com/a", "https://x.com/a")]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("//evil.com", null)]
    [InlineData("\\\\evil.com", null)]
    [InlineData("  ", null)]
    public void Resolves_to_the_expected_stored_value(string input, string? expected)
    {
        Assert.Equal(expected, ContentService.SafeHref(input));
    }
}
