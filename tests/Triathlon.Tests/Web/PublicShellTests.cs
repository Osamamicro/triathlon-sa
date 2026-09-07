using System.Net;

namespace Triathlon.Tests.Web;

/// <summary>Week 1 rulings on the shared shell: one culture per document, favicon, heading order.</summary>
[Collection(WebAppCollection.Name)]
public sealed class PublicShellTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/en")]
    [InlineData("/ar")]
    public async Task Server_markup_carries_no_paired_language_spans(string path)
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync(path);

        Assert.DoesNotContain("class=\"en\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"ar\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Arabic_home_is_arabic_and_english_home_is_english()
    {
        using var client = app.CreateClient();
        var english = await client.GetStringAsync("/en");
        var arabic = await client.GetStringAsync("/ar");

        Assert.Contains("Find your race", english, StringComparison.Ordinal);
        Assert.DoesNotContain("اعثر على سباقك", english, StringComparison.Ordinal);
        Assert.Contains("اعثر على سباقك", arabic, StringComparison.Ordinal);
        Assert.DoesNotContain("Find your race", arabic, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upper_case_culture_segment_still_renders_that_culture()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/AR");

        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", html, StringComparison.Ordinal);
        Assert.Contains("اعثر على سباقك", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Favicon_ico_exists()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/favicon.ico");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Footer_columns_are_h3()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en");
        var footer = html[html.IndexOf("<footer", StringComparison.Ordinal)..];

        Assert.DoesNotContain("<h4", footer, StringComparison.Ordinal);
        Assert.Contains("<h3>Compete</h3>", footer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_inline_scripts_on_the_home_page()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en");

        // CSP is script-src 'self': an inline script would be blocked silently in the browser.
        Assert.False(Markup.HasInlineScript(html));
    }
}
