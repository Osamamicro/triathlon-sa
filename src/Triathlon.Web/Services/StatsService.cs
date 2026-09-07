using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Services;

/// <summary>Everything the <c>/statistics</c> page renders: all KPI tiles, region bars and the growth series.</summary>
public sealed record StatsOverview(IReadOnlyList<Kpi> Kpis, IReadOnlyList<RegionStat> Regions, IReadOnlyList<GrowthPoint> Growth);

/// <summary>Queries over the statistics domain — the home page's four-tile band and the full statistics page.</summary>
public sealed class StatsService(AppDbContext db)
{
    /// <summary>The four KPIs flagged for the home page, in their own <see cref="Kpi.HomeOrder"/>.</summary>
    public async Task<IReadOnlyList<Kpi>> HomeKpisAsync(CancellationToken ct) =>
        await db.Kpis.AsNoTracking().Where(k => k.ShowOnHome).OrderBy(k => k.HomeOrder).ToListAsync(ct);

    /// <summary>All ten KPIs, the six region counts and the four-year growth series, each in the prototype's own order.</summary>
    public async Task<StatsOverview> AllAsync(CancellationToken ct)
    {
        var kpis = await db.Kpis.AsNoTracking().OrderBy(k => k.SortOrder).ToListAsync(ct);
        var regions = await db.RegionStats.AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(ct);
        var growth = await db.GrowthPoints.AsNoTracking().OrderBy(g => g.Year).ToListAsync(ct);
        return new StatsOverview(kpis, regions, growth);
    }
}
