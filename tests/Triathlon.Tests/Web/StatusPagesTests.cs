using System.Net;
using Microsoft.Extensions.Configuration;

namespace Triathlon.Tests.Web;

/// <summary>
/// A visitor who mistypes a public URL must land on the Federation's own page, in the language they
/// were reading, and a crawler must still see a 404 — so these cover both the body and the status
/// code, per area, plus the paths that should get no page at all.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class StatusPagesTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/en/no-such-page", "en", "Page not found")]
    [InlineData("/ar/no-such-page", "ar", "الصفحة غير موجودة")]
    [InlineData("/fr", "en", "Page not found")]
    public async Task Public_404_is_branded_and_in_the_visitors_culture(string path, string lang, string copy)
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains($"<html lang=\"{lang}\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"site-header\"", html, StringComparison.Ordinal);
        Assert.Contains(copy, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Direct_visit_to_the_404_page_is_a_404()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/en/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_404_stays_on_the_dashboard_page()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/dashboard/no-such-page");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("class=\"site-header\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/css/missing.css")]
    [InlineData("/api/missing")]
    public async Task Files_and_api_paths_get_a_bare_404(string path)
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<html", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Staging_site_asks_not_to_be_indexed_and_production_does_not()
    {
        using var production = app.CreateClient();
        Assert.DoesNotContain("name=\"robots\"", await production.GetStringAsync("/en"), StringComparison.Ordinal);

        using var staging = app.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Site:Staging"] = "true",
                ["Site:BasicAuth:User"] = "stf",
                ["Site:BasicAuth:Password"] = "staging-pass-2026",
            })));
        using var client = staging.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("stf:staging-pass-2026")));

        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", await client.GetStringAsync("/en"), StringComparison.Ordinal);
    }
}
