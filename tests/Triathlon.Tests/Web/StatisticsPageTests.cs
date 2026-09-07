namespace Triathlon.Tests.Web;

[Collection(WebAppCollection.Name)]
public sealed class StatisticsPageTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/en/statistics", "Federation Statistics", "data-count=\"1284\"", "Riyadh")]
    [InlineData("/ar/statistics", "إحصائيات الاتحاد", "data-count=\"1284\"", "الرياض")]
    public async Task Statistics_page_renders_tiles_and_bars(string path, string title, string tile, string region)
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync(path);
        Assert.Contains(title, html, StringComparison.Ordinal);
        Assert.Contains(tile, html, StringComparison.Ordinal);
        Assert.Contains(region, html, StringComparison.Ordinal);
        Assert.Contains("class=\"bar-fill\" data-w=\"100\"", html, StringComparison.Ordinal);
        Assert.False(Markup.HasInlineScript(html));
    }

    /// <summary>
    /// The figures and the bar widths are in the served HTML, not only in the data attributes the
    /// counter reads. A crawler, a printout and a reduced-motion visitor see the real numbers; the
    /// count-up in site.js resets them to zero itself before it animates.
    /// </summary>
    [Theory]
    [InlineData("/en/statistics", "data-count=\"1284\">1,284")]
    [InlineData("/ar/statistics", "data-count=\"1284\">١٬٢٨٤")]
    public async Task Statistics_serve_the_real_figures_not_zero(string path, string rendered)
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync(path);

        Assert.Contains(rendered, html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-count=\"1284\">0", html, StringComparison.Ordinal);
        Assert.Contains("data-w=\"100\" style=\"width:100%\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Home_band_reads_kpis_from_the_database()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/ar");
        Assert.Contains("رياضي مسجل", html, StringComparison.Ordinal);
        Assert.Contains("data-count=\"18650\"", html, StringComparison.Ordinal);
    }
}
