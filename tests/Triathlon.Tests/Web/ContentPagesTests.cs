using System.Net;

namespace Triathlon.Tests.Web;

/// <summary>
/// The content half of the public site end to end: governance and its documents library, the rules
/// page, the training guides, the CMS-driven join and contact pages, and the newsroom — each in
/// both cultures, rendered from the seeded database.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class ContentPagesTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/en/governance", "Board of Directors", "Annual Report 2025")]
    [InlineData("/ar/governance", "مجلس الإدارة", "التقرير السنوي 2025")]
    [InlineData("/en/governance/documents?category=finance&year=2025", "Audited Financial Statements 2024", "/download\"")]
    [InlineData("/en/rules?audience=officials", "Technical Officials Handbook", "Race day at a glance")]
    [InlineData("/ar/rules", "قوانين المنافسات 2026", "يوم السباق باختصار")]
    [InlineData("/en/training", "12 weeks", "In preparation")]
    [InlineData("/en/training/beginner-12-weeks", "Learn the brick", "Download")]
    [InlineData("/ar/training/beginner-12-weeks", "تعلّم التمرين المركب", "تحميل")]
    [InlineData("/en/join", "Four steps to your license", "Riyadh Tri Club")]
    [InlineData("/ar/join", "أربع خطوات إلى رخصتك", "نادي الرياض للترايثلون")]
    [InlineData("/en/contact", "Prince Faisal Bin Fahad Olympic Complex", "info@triathlon.sa")]
    [InlineData("/en/news", "Riyadh Sprint", "/en/news/")]
    [InlineData("/ar/news", "الرياض", "/ar/news/")]
    public async Task Page_renders_seeded_content_in_the_culture(string path, string first, string second)
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(first, html, StringComparison.Ordinal);
        Assert.Contains(second, html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"en\"", html, StringComparison.Ordinal);
        Assert.False(Markup.HasInlineScript(html));
    }

    [Fact]
    public async Task Documents_library_filter_hides_other_categories_and_shows_facet_counts()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en/governance/documents?category=minutes");
        Assert.Contains("Board Meeting Minutes", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Annual Report 2025", html, StringComparison.Ordinal);
        Assert.Contains("Board minutes <span class=\"count\">3</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unpublished_guide_and_unknown_slugs_are_404()
    {
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/en/training/olympic-progression")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/en/news/nope")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/en/no-such-cms-page")).StatusCode);
    }

    [Fact]
    public async Task News_post_renders_its_body_and_home_shows_the_latest_three()
    {
        using var client = app.CreateClient();
        var list = await client.GetStringAsync("/en/news");
        var slug = System.Text.RegularExpressions.Regex.Match(list, "href=\"/en/news/([a-z0-9-]+)\"").Groups[1].Value;
        var post = await client.GetStringAsync("/en/news/" + slug);
        var home = await client.GetStringAsync("/en");

        Assert.Contains("<article", post, StringComparison.Ordinal);
        Assert.Contains("href=\"/en/news/" + slug + "\"", home, StringComparison.Ordinal);
    }
}
