using System.Globalization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Crm;
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

    [Fact]
    public async Task Saving_regions_with_a_duplicated_key_in_the_list_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
        var regions = await stats.RegionsForEditAsync(CancellationToken.None);

        var inputs = regions.Select(ToInput).ToList();
        inputs[1] = inputs[1] with { Key = inputs[0].Key };

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            stats.SaveRegionsAsync(inputs, CancellationToken.None));
        Assert.Equal("Regions", ex.Field);
        Assert.Equal("Validation_KeyDuplicate", ex.Key);
    }

    [Fact]
    public async Task A_removed_regions_key_stays_reserved_until_the_row_is_restored()
    {
        RegionStat removed;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
            var regions = await stats.RegionsForEditAsync(CancellationToken.None);
            removed = regions[^1];

            var withoutRemoved = regions.Where(r => r.Id != removed.Id).Select(ToInput).ToList();
            await stats.SaveRegionsAsync(withoutRemoved, CancellationToken.None);
        }

        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
            var remaining = await stats.RegionsForEditAsync(CancellationToken.None);

            // Posting the removed region back as a brand-new row (null id, same key) must be refused:
            // the row is only soft-deleted, and IX_RegionStats_Key is an unfiltered unique index.
            var reposted = remaining.Select(ToInput)
                .Append(new RegionInput(null, removed.Key, removed.NameEn, removed.NameAr, removed.Athletes, removed.SortOrder))
                .ToList();

            var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
                stats.SaveRegionsAsync(reposted, CancellationToken.None));
            Assert.Equal("Regions", ex.Field);
            Assert.Equal("Validation_KeyDuplicate", ex.Key);
        }
        finally
        {
            await using var restoreScope = app.Services.CreateAsyncScope();
            var db = restoreScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.RegionStats.IgnoreQueryFilters().SingleAsync(r => r.Id == removed.Id);
            row.DeletedAt = null;
            await db.SaveChangesAsync(CancellationToken.None);

            // Direct db write, not a SaveRegionsAsync/commit.ApplyAsync call, so nothing else evicted
            // the statistics tag on the way out.
            await restoreScope.ServiceProvider.GetRequiredService<IOutputCacheStore>()
                .EvictAsync(CancellationToken.None, CacheTags.Stats);
        }
    }

    [Fact]
    public async Task Saving_growth_with_a_stale_id_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
        var growth = await stats.GrowthForEditAsync(CancellationToken.None);

        var inputs = growth.Select(ToInput).ToList();
        inputs[0] = inputs[0] with { Id = Guid.NewGuid() };

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            stats.SaveGrowthAsync(inputs, CancellationToken.None));
        Assert.Equal("Growth", ex.Field);
        Assert.Equal("Validation_NotFound", ex.Key);
    }

    [Fact]
    public async Task Saving_growth_evicts_statistics_and_shows_the_new_value()
    {
        using var client = app.CreateClient();
        _ = await client.GetStringAsync("/en/statistics");
        Assert.True((await client.GetAsync("/en/statistics")).Headers.Contains("Age"));

        IReadOnlyList<GrowthPoint> original;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
            original = await stats.GrowthForEditAsync(CancellationToken.None);
        }

        try
        {
            var newAthletes = original[0].Athletes + 5000;
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
                var inputs = original.Select(ToInput).ToList();
                inputs[0] = inputs[0] with { Athletes = newAthletes };
                await stats.SaveGrowthAsync(inputs, CancellationToken.None);
            }

            using var after = await client.GetAsync("/en/statistics");
            Assert.False(after.Headers.Contains("Age"), "statistics page was not evicted by the growth save");

            var page = await after.Content.ReadAsStringAsync();
            Assert.Contains(newAthletes.ToString("N0", CultureInfo.InvariantCulture), page, StringComparison.Ordinal);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
            await stats.SaveGrowthAsync(original.Select(ToInput).ToList(), CancellationToken.None);
        }
    }

    [Fact]
    public async Task Deleting_and_restoring_an_athlete_moves_it_in_and_out_of_the_deleted_list()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crm = scope.ServiceProvider.GetRequiredService<CrmService>();

        var athlete = new Athlete
        {
            FullName = "Delete Restore Test",
            Email = $"delete-restore-{Guid.NewGuid():N}@example.test",
            Category = AthleteCategories.AgeGroup,
            Status = AthleteStatus.Pending,
        };
        db.Athletes.Add(athlete);
        await db.SaveChangesAsync(CancellationToken.None);

        await crm.DeleteAthleteAsync(athlete.Id, CancellationToken.None);

        var deleted = await crm.AthletesForEditAsync(null, deletedOnly: true, CancellationToken.None);
        Assert.Contains(deleted, a => a.Id == athlete.Id);

        await crm.RestoreAthleteAsync(athlete.Id, CancellationToken.None);

        var stillDeleted = await crm.AthletesForEditAsync(null, deletedOnly: true, CancellationToken.None);
        Assert.DoesNotContain(stillDeleted, a => a.Id == athlete.Id);

        var live = await crm.AthletesForEditAsync(null, deletedOnly: false, CancellationToken.None);
        Assert.Contains(live, a => a.Id == athlete.Id);
    }

    private static KpiInput ToInput(Kpi kpi) => new(
        kpi.Id, kpi.LabelEn, kpi.LabelAr, kpi.Value, kpi.Suffix, kpi.ShowPlus,
        kpi.NoteEn, kpi.NoteAr, kpi.Color, kpi.SortOrder, kpi.ShowOnHome, kpi.HomeOrder, kpi.Source);

    private static RegionInput ToInput(RegionStat region) =>
        new(region.Id, region.Key, region.NameEn, region.NameAr, region.Athletes, region.SortOrder);

    private static GrowthInput ToInput(GrowthPoint growth) =>
        new(growth.Id, growth.Year, growth.Athletes);
}
