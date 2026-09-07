using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Services;
using Triathlon.Tests.Web;

namespace Triathlon.Tests.Services;

[Collection(WebAppCollection.Name)]
public sealed class StatsServiceTests(WebAppFixture app)
{
    [Fact]
    public async Task Home_kpis_are_the_four_flagged_ones_in_order()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var home = await scope.ServiceProvider.GetRequiredService<StatsService>().HomeKpisAsync(CancellationToken.None);
        Assert.Equal(["athletes", "elite", "tournaments", "participants"], home.Select(k => k.Key));
    }

    [Fact]
    public async Task All_returns_ten_kpis_six_regions_and_four_growth_points()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var all = await scope.ServiceProvider.GetRequiredService<StatsService>().AllAsync(CancellationToken.None);
        Assert.Equal(10, all.Kpis.Count);
        Assert.Equal(6, all.Regions.Count);
        Assert.Equal([2023, 2024, 2025, 2026], all.Growth.Select(g => g.Year));
    }
}
