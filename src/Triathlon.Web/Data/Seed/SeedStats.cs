using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Transcribes the prototype's statistics (<c>assets/js/data.js</c> lines 10–37) into the database:
/// the ten KPI tiles' real values, the six region-athlete counts and the four-year growth series.
/// <see cref="SeedStructure"/> creates the ten KPI rows themselves (started at
/// <c>Value = 0</c>, in the same keys/labels/sort order used below) before this ever runs, so this
/// class only ever updates existing rows — it never inserts a <see cref="Kpi"/>.
/// <para>
/// Each of the three tables here guards itself independently: the KPI values are only set while
/// every KPI still reads zero (so a real value the dashboard or the nightly computed-KPI job has
/// since written is never stomped on), and regions/growth are each guarded on their own, empty table.
/// </para>
/// </summary>
public static class SeedStats
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct)
    {
        await KpiValuesAsync(db, ct);
        await RegionsAsync(db, ct);
        await GrowthAsync(db, ct);
    }

    private static async Task KpiValuesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Kpis.AnyAsync(k => k.Value != 0, ct)) return;

        var values = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["athletes"] = 1284,
            ["elite"] = 42,
            ["participants"] = 18650,
            ["tournaments"] = 24,
            ["community"] = 46,
            ["clubs"] = 17,
            ["regions"] = 9,
            ["volunteers"] = 380,
            ["women"] = 31,
            ["youth"] = 27,
        };

        var kpis = await db.Kpis.ToListAsync(ct);
        var changed = false;
        foreach (var kpi in kpis)
        {
            if (values.TryGetValue(kpi.Key, out var value))
            {
                kpi.Value = value;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task RegionsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.RegionStats.IgnoreQueryFilters().AnyAsync(ct)) return;

        RegionStat R(string key, (string En, string Ar) name, int athletes, int sortOrder) => new()
        {
            Key = key, NameEn = name.En, NameAr = name.Ar, Athletes = athletes, SortOrder = sortOrder,
        };

        db.RegionStats.AddRange(
            R("riyadh", ("Riyadh", "الرياض"), 512, 1),
            R("makkah", ("Makkah (Jeddah)", "مكة المكرمة (جدة)"), 341, 2),
            R("eastern", ("Eastern Province", "المنطقة الشرقية"), 214, 3),
            R("madinah", ("Madinah / Yanbu", "المدينة / ينبع"), 96, 4),
            R("asir", ("Asir (Abha)", "عسير (أبها)"), 62, 5),
            R("tabuk", ("Tabuk / NEOM", "تبوك / نيوم"), 59, 6));

        await db.SaveChangesAsync(ct);
    }

    private static async Task GrowthAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.GrowthPoints.IgnoreQueryFilters().AnyAsync(ct)) return;

        db.GrowthPoints.AddRange(
            new GrowthPoint { Year = 2023, Athletes = 310 },
            new GrowthPoint { Year = 2024, Athletes = 640 },
            new GrowthPoint { Year = 2025, Athletes = 980 },
            new GrowthPoint { Year = 2026, Athletes = 1284 });

        await db.SaveChangesAsync(ct);
    }
}
