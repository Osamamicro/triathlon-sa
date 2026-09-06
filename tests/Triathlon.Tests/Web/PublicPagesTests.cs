using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Triathlon.Tests.Web;

/// <summary>
/// The public site is one set of pages served under a culture segment, so these cover the pieces
/// that segment is responsible for: the document direction, the root redirect that picks a culture
/// for a first-time visitor, the rejection of anything that is not a supported culture, and the
/// language toggle that has to point at the other culture with the other culture's own name on it.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class PublicPagesTests(WebAppFixture app)
{
    [Fact]
    public async Task En_home_renders_ltr()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/en");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"en\" dir=\"ltr\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ar_home_renders_rtl()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/ar");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "/en")]
    [InlineData("ar-SA,ar;q=0.9", "/ar")]
    [InlineData("en-GB,en;q=0.9", "/en")]
    [InlineData("fr-FR,fr;q=0.9", "/en")]
    // A lower-weighted Arabic must not beat the visitor's actual first choice.
    [InlineData("en-US,en;q=0.9,ar;q=0.8", "/en")]
    [InlineData("en;q=0.4,ar;q=0.9", "/ar")]
    // Neither top choice is published, so the best supported one wins rather than the default.
    [InlineData("fr-FR,fr;q=0.9,ar;q=0.5", "/ar")]
    // "arn" (Mapudungun) must not match "ar" on a bare prefix check.
    [InlineData("arn", "/en")]
    // q=0 means "not acceptable", not "low priority" — it must never win.
    [InlineData("ar;q=0,en;q=0.5", "/en")]
    public async Task Root_redirects_by_accept_language(string? acceptLanguage, string expected)
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location!.ToString());
        // The redirect target depends on the request's Accept-Language, and the choice itself
        // must not be cached, so a shared cache needs both signals.
        Assert.Contains("Accept-Language", response.Headers.Vary);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Unknown_culture_is_404()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/fr");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Language_toggle_links_to_other_culture()
    {
        using var client = app.CreateClient();

        var english = await client.GetStringAsync("/en");
        var arabic = await client.GetStringAsync("/ar");

        Assert.Contains("href=\"/ar\"", english, StringComparison.Ordinal);
        Assert.Contains("العربية", english, StringComparison.Ordinal);
        Assert.Contains("class=\"lang-toggle\"", english, StringComparison.Ordinal);

        Assert.Contains("href=\"/en\"", arabic, StringComparison.Ordinal);
        Assert.Contains("class=\"lang-toggle\"", arabic, StringComparison.Ordinal);

        // The visible label is the other language's own name, which is the whole point of the
        // control: an Arabic page must offer "English", never "العربية".
        Assert.Contains(">English<", LangToggle(arabic), StringComparison.Ordinal);
        Assert.Contains(">العربية<", LangToggle(english), StringComparison.Ordinal);
    }

    /// <summary>Isolates the language toggle anchor so the assertions cannot pass on stray page copy.</summary>
    private static string LangToggle(string html)
    {
        var start = html.IndexOf("class=\"lang-toggle\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "The page has no language toggle.");

        var end = html.IndexOf("</a>", start, StringComparison.Ordinal);
        Assert.True(end > start, "The language toggle is not an anchor.");

        return html[start..(end + "</a>".Length)];
    }

    [Fact]
    public async Task Theme_toggle_label_is_localized()
    {
        using var client = app.CreateClient();

        var english = await client.GetStringAsync("/en");
        var arabic = await client.GetStringAsync("/ar");

        var englishToggle = ThemeToggle(english);
        Assert.Contains("data-label-light=\"Switch to light mode\"", englishToggle, StringComparison.Ordinal);
        Assert.Contains("data-label-dark=\"Switch to dark mode\"", englishToggle, StringComparison.Ordinal);

        var arabicToggle = ThemeToggle(arabic);
        Assert.Contains("data-label-light=\"التبديل إلى الوضع الفاتح\"", arabicToggle, StringComparison.Ordinal);
        Assert.Contains("data-label-dark=\"التبديل إلى الوضع الداكن\"", arabicToggle, StringComparison.Ordinal);
        // The whole point: an Arabic page must never carry the hard-coded English strings that
        // site.js used to stamp onto the toggle's aria-label after boot.
        Assert.DoesNotContain("Switch to", arabicToggle, StringComparison.Ordinal);
    }

    /// <summary>Isolates the theme toggle button so the assertions cannot pass on stray page copy.</summary>
    private static string ThemeToggle(string html)
    {
        var start = html.IndexOf("class=\"theme-toggle\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "The page has no theme toggle.");

        var end = html.IndexOf("</button>", start, StringComparison.Ordinal);
        Assert.True(end > start, "The theme toggle is not a button.");

        return html[start..(end + "</button>".Length)];
    }
}
