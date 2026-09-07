using Microsoft.Extensions.Configuration;

namespace Triathlon.Tests.Web;

/// <summary>
/// SEO surface: per-page <c>&lt;head&gt;</c> metadata (canonical, hreflang alternates, description,
/// JSON-LD), the sitemap and robots.txt.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class SeoTests(WebAppFixture app)
{
    [Fact]
    public async Task Pages_carry_canonical_hreflang_and_description()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en/events/riyadh-sprint-2026?utm=x");
        Assert.Contains("<link rel=\"canonical\" href=\"http://localhost/en/events/riyadh-sprint-2026\">", html, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"alternate\" hreflang=\"ar\" href=\"http://localhost/ar/events/riyadh-sprint-2026\">", html, StringComparison.Ordinal);
        Assert.Contains("hreflang=\"x-default\"", html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"description\" content=\"Round 1 of the national series.", html, StringComparison.Ordinal);
        Assert.Contains("\"@type\":\"SportsEvent\"", html, StringComparison.Ordinal);
        Assert.Contains("\"startDate\":\"2026-10-17T06:00:00+03:00\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sitemap_lists_both_cultures_with_alternates_and_robots_points_at_it()
    {
        using var client = app.CreateClient();
        var sitemap = await client.GetStringAsync("/sitemap.xml");
        var robots = await client.GetStringAsync("/robots.txt");
        Assert.Contains("<loc>http://localhost/ar/events/riyadh-sprint-2026</loc>", sitemap, StringComparison.Ordinal);
        Assert.Contains("hreflang=\"en\" href=\"http://localhost/en/events/riyadh-sprint-2026\"", sitemap, StringComparison.Ordinal);
        Assert.DoesNotContain("/register/received", sitemap, StringComparison.Ordinal);
        Assert.Contains("Sitemap: http://localhost/sitemap.xml", robots, StringComparison.Ordinal);
        Assert.Contains("Disallow: /dashboard", robots, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Staging_robots_disallows_everything()
    {
        using var staging = app.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Site:Staging"] = "true", ["Site:BasicAuth:User"] = "stf", ["Site:BasicAuth:Password"] = "staging-pass-2026" })));
        using var client = staging.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("stf:staging-pass-2026")));
        Assert.Contains("Disallow: /\n", await client.GetStringAsync("/robots.txt"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inline_script_helper_allows_json_ld_but_still_catches_a_real_inline_script()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en/events/riyadh-sprint-2026");

        // The event page now carries a JSON-LD script tag; it must not trip the CSP-inline check.
        Assert.False(Markup.HasInlineScript(html));

        // A hand-written inline script — the shape CSP's script-src 'self' actually blocks — must
        // still be caught.
        Assert.True(Markup.HasInlineScript("<script>alert(1)</script>"));
    }
}
