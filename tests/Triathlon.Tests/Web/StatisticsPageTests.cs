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
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
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
