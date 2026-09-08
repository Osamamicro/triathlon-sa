using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Stats;
using Triathlon.Web.Jobs;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Jobs;

/// <summary>
/// The nightly recompute: a KPI flagged <see cref="KpiSource.Computed"/> is refreshed from the live
/// tables, the change is one audited "compute" row stamped as the system user, and the statistics
/// page (which is output-cached under <see cref="Triathlon.Web.Services.CacheTags.Stats"/>) reflects
/// the new number immediately rather than on the next natural eviction.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class ComputedKpisJobTests(WebAppFixture app)
{
    [Fact]
    public async Task Running_the_job_recomputes_a_computed_kpi_and_evicts_statistics()
    {
        using var client = app.CreateClient();
        _ = await client.GetStringAsync("/en/statistics");
        Assert.True((await client.GetAsync("/en/statistics")).Headers.Contains("Age"));

        KpiSource originalSource;
        long originalValue;

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var clubs = await db.Kpis.SingleAsync(k => k.Key == "clubs");
            originalSource = clubs.Source;
            originalValue = clubs.Value;
            clubs.Source = KpiSource.Computed;
            clubs.Value = 0;
            await db.SaveChangesAsync(CancellationToken.None);
        }

        try
        {
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var job = scope.ServiceProvider.GetRequiredService<ComputedKpisJob>();
                await job.RunAsync(CancellationToken.None);
            }

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var clubs = await db.Kpis.AsNoTracking().SingleAsync(k => k.Key == "clubs");
                Assert.Equal(6, clubs.Value); // the six clubs SeedClubs seeds, all IsActive

                var log = await db.ActivityLogs
                    .Where(l => l.Entity == "Kpi" && l.Action == "compute")
                    .OrderByDescending(l => l.At)
                    .FirstOrDefaultAsync();
                Assert.NotNull(log);
                Assert.Equal("system", log!.User);
                Assert.Contains("clubs", log.Diff, StringComparison.Ordinal);
            }

            var page = await client.GetStringAsync("/en/statistics");
            Assert.Contains("data-count=\"6\"", page, StringComparison.Ordinal);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var clubs = await db.Kpis.SingleAsync(k => k.Key == "clubs");
            clubs.Source = originalSource;
            clubs.Value = originalValue;
            await db.SaveChangesAsync(CancellationToken.None);

            // Direct db write, not a SaveKpiAsync/commit.ApplyAsync call, so nothing else evicted the
            // statistics tag on the way out — this test's own "6" would otherwise outlive it.
            await scope.ServiceProvider.GetRequiredService<IOutputCacheStore>().EvictAsync(CancellationToken.None, CacheTags.Stats);
        }
    }
}
