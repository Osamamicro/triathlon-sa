using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Stats;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// The write half of the statistics domain: a KPI save evicts the home page (four of the ten KPIs
/// show there) as well as the statistics page itself, and the home band's four positions cannot
/// collide.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class StatsWriteTests(WebAppFixture app)
{
    [Fact]
    public async Task Saving_a_kpi_value_evicts_the_home_page_and_shows_the_new_number()
    {
        using var client = app.CreateClient();
        _ = await client.GetStringAsync("/en");
        Assert.True((await client.GetAsync("/en")).Headers.Contains("Age"));

        Kpi original;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            original = await db.Kpis.AsNoTracking().SingleAsync(k => k.Key == "athletes");
        }

        try
        {
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
                await stats.SaveKpiAsync(ToInput(original) with { Value = 1300 }, CancellationToken.None);
            }

            using var after = await client.GetAsync("/en");
            Assert.False(after.Headers.Contains("Age"), "home page was not evicted by the KPI save");

            var page = await after.Content.ReadAsStringAsync();
            Assert.Contains("1,300", page, StringComparison.Ordinal);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
            await stats.SaveKpiAsync(ToInput(original), CancellationToken.None);
        }
    }

    [Fact]
    public async Task Two_home_kpis_cannot_share_a_home_order()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var elite = await db.Kpis.AsNoTracking().SingleAsync(k => k.Key == "elite");
        var stats = scope.ServiceProvider.GetRequiredService<StatsService>();

        // "athletes" already sits on HomeOrder 1; pointing "elite" (normally HomeOrder 2) at the
        // same position must be refused rather than silently taking it over.
        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            stats.SaveKpiAsync(ToInput(elite) with { HomeOrder = 1 }, CancellationToken.None));
        Assert.Equal("HomeOrder", ex.Field);

        var unchanged = await db.Kpis.AsNoTracking().SingleAsync(k => k.Key == "elite");
        Assert.Equal(elite.HomeOrder, unchanged.HomeOrder);
    }

    private static KpiInput ToInput(Kpi kpi) => new(
        kpi.Id, kpi.LabelEn, kpi.LabelAr, kpi.Value, kpi.Suffix, kpi.ShowPlus,
        kpi.NoteEn, kpi.NoteAr, kpi.Color, kpi.SortOrder, kpi.ShowOnHome, kpi.HomeOrder, kpi.Source);
}
