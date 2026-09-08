using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Services;

/// <summary>Everything the <c>/statistics</c> page renders: all KPI tiles, region bars and the growth series.</summary>
public sealed record StatsOverview(IReadOnlyList<Kpi> Kpis, IReadOnlyList<RegionStat> Regions, IReadOnlyList<GrowthPoint> Growth);

/// <summary>
/// Queries over the statistics domain — the home page's four-tile band and the full statistics
/// page — plus the dashboard's write side: KPI edits, and whole-list replaces for the region and
/// growth charts (the same "rows keep their ids, order is the list's order, rows left out are
/// deleted" pattern as <c>ContentService.SaveNavigationAsync</c>).
/// </summary>
public sealed class StatsService(AppDbContext db, ContentCommit commit)
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

    // ------------------------------------------------------------------- kpis

    public async Task<IReadOnlyList<Kpi>> KpisForEditAsync(CancellationToken ct) =>
        await db.Kpis.AsNoTracking().OrderBy(k => k.SortOrder).ToListAsync(ct);

    /// <summary>
    /// Updates one KPI. Rows are never created or deleted from the dashboard — the key set is
    /// structural — so the save always targets an existing row by <see cref="KpiInput.Id"/>.
    /// <see cref="KpiInput.HomeOrder"/> only matters while <see cref="KpiInput.ShowOnHome"/> is set,
    /// and must then land on one of the four home-band positions with no other home KPI already
    /// sitting on it — validated before anything is assigned, same two-pass shape as every other
    /// write service.
    /// </summary>
    public async Task<Kpi> SaveKpiAsync(KpiInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var row = await db.Kpis.SingleOrDefaultAsync(k => k.Id == input.Id, ct)
            ?? throw new ContentValidationException("Id", "Validation_NotFound");

        // ---- pass 1: validate only — no property assignment below this point ----
        var labelEn = Required(input.LabelEn, "LabelEn");
        var labelAr = Required(input.LabelAr, "LabelAr");

        if (input.ShowOnHome)
        {
            if (input.HomeOrder is < 1 or > 4)
                throw new ContentValidationException("HomeOrder", "Validation_HomeOrder");
            if (await db.Kpis.AnyAsync(k => k.Id != input.Id && k.ShowOnHome && k.HomeOrder == input.HomeOrder, ct))
                throw new ContentValidationException("HomeOrder", "Validation_HomeOrder");
        }

        // ---- pass 2: every check above passed — assign ----
        var before = Audit.Snapshot(row);
        row.LabelEn = labelEn; row.LabelAr = labelAr;
        row.Value = input.Value;
        row.Suffix = Blank(input.Suffix);
        row.ShowPlus = input.ShowPlus;
        row.NoteEn = Blank(input.NoteEn); row.NoteAr = Blank(input.NoteAr);
        row.Color = Blank(input.Color);
        row.SortOrder = input.SortOrder;
        row.ShowOnHome = input.ShowOnHome;
        row.HomeOrder = input.ShowOnHome ? input.HomeOrder : 0;
        row.Source = input.Source;

        await commit.ApplyAsync("Kpi", row.Id, "update", before, Audit.Snapshot(row), [CacheTags.Stats, CacheTags.Home], ct);
        return row;
    }

    // ---------------------------------------------------------------- regions

    public async Task<IReadOnlyList<RegionStat>> RegionsForEditAsync(CancellationToken ct) =>
        await db.RegionStats.AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(ct);

    /// <summary>Replaces the whole region list: rows keep their ids, rows left out are deleted.</summary>
    public async Task SaveRegionsAsync(IReadOnlyList<RegionInput> items, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(items);
        var existing = await db.RegionStats.ToListAsync(ct);
        var before = existing.OrderBy(r => r.SortOrder).Select(Audit.Snapshot).ToList();

        // ---- pass 1: validate every input — no row is mutated or added below this point ----
        var seenIds = new HashSet<Guid>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var prepared = new List<(RegionInput Input, string NameEn, string NameAr)>(items.Count);
        foreach (var input in items)
        {
            if (input.Id is { } dupId && !seenIds.Add(dupId))
                throw new ContentValidationException("Regions", "Validation_DuplicateRow");
            if (!Slugs.IsValid(input.Key))
                throw new ContentValidationException("Key", "Validation_SlugFormat");
            if (!seenKeys.Add(input.Key))
                throw new ContentValidationException("Key", "Validation_KeyDuplicate");
            prepared.Add((input, Required(input.NameEn, "NameEn"), Required(input.NameAr, "NameAr")));
        }

        // ---- pass 2: every check above passed — assign and upsert rows ----
        var keep = new HashSet<Guid>();
        var saved = new List<RegionStat>();
        foreach (var (input, nameEn, nameAr) in prepared)
        {
            var row = input.Id is { } id ? existing.FirstOrDefault(r => r.Id == id) : null;
            if (row is null)
            {
                row = new RegionStat { Key = input.Key, NameEn = "", NameAr = "" };
                db.RegionStats.Add(row);
            }

            keep.Add(row.Id);
            row.Key = input.Key;
            row.NameEn = nameEn; row.NameAr = nameAr;
            row.Athletes = input.Athletes;
            row.SortOrder = input.SortOrder;
            saved.Add(row);
        }

        db.RegionStats.RemoveRange(existing.Where(r => !keep.Contains(r.Id)));
        var after = saved.OrderBy(r => r.SortOrder).Select(Audit.Snapshot).ToList();
        await commit.ApplyAsync("RegionStat", Guid.Empty, "update", before, after, [CacheTags.Stats, CacheTags.Home], ct);
    }

    // ----------------------------------------------------------------- growth

    public async Task<IReadOnlyList<GrowthPoint>> GrowthForEditAsync(CancellationToken ct) =>
        await db.GrowthPoints.AsNoTracking().OrderBy(g => g.Year).ToListAsync(ct);

    /// <summary>Replaces the whole growth series: rows keep their ids, rows left out are deleted.</summary>
    public async Task SaveGrowthAsync(IReadOnlyList<GrowthInput> items, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(items);
        var existing = await db.GrowthPoints.ToListAsync(ct);
        var before = existing.OrderBy(g => g.Year).Select(Audit.Snapshot).ToList();

        // ---- pass 1: validate every input — no row is mutated or added below this point ----
        var seenIds = new HashSet<Guid>();
        var seenYears = new HashSet<int>();
        foreach (var input in items)
        {
            if (input.Id is { } dupId && !seenIds.Add(dupId))
                throw new ContentValidationException("Growth", "Validation_DuplicateRow");
            if (!seenYears.Add(input.Year))
                throw new ContentValidationException("Year", "Validation_YearDuplicate");
        }

        // ---- pass 2: every check above passed — assign and upsert rows ----
        var keep = new HashSet<Guid>();
        var saved = new List<GrowthPoint>();
        foreach (var input in items)
        {
            var row = input.Id is { } id ? existing.FirstOrDefault(g => g.Id == id) : null;
            if (row is null)
            {
                row = new GrowthPoint();
                db.GrowthPoints.Add(row);
            }

            keep.Add(row.Id);
            row.Year = input.Year;
            row.Athletes = input.Athletes;
            saved.Add(row);
        }

        db.GrowthPoints.RemoveRange(existing.Where(g => !keep.Contains(g.Id)));
        var after = saved.OrderBy(g => g.Year).Select(Audit.Snapshot).ToList();
        await commit.ApplyAsync("GrowthPoint", Guid.Empty, "update", before, after, [CacheTags.Stats, CacheTags.Home], ct);
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new ContentValidationException(field, "Validation_Required") : value.Trim();

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
