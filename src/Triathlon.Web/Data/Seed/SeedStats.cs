using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Transcribes the prototype's statistics (<c>assets/js/data.js</c> lines 10–37) into the database:
/// the ten KPI tiles, the six region-athlete counts and the four-year growth series. Idempotent:
/// skips entirely once any KPI exists.
/// </summary>
public static class SeedStats
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Kpis.AnyAsync(ct)) return;

        Kpi K(string key, long value, (string En, string Ar) label, int sortOrder,
            string? suffix = null, bool showPlus = false, (string En, string Ar)? note = null,
            string? color = null, bool showOnHome = false, int homeOrder = 0) => new()
        {
            Key = key, LabelEn = label.En, LabelAr = label.Ar, Value = value, SortOrder = sortOrder,
            Suffix = suffix, ShowPlus = showPlus, NoteEn = note?.En, NoteAr = note?.Ar,
            Color = color, ShowOnHome = showOnHome, HomeOrder = homeOrder, Source = KpiSource.Manual,
        };

        var kpis = new List<Kpi>
        {
            K("athletes", 1284, ("Registered athletes", "رياضي مسجل"), 1,
                showPlus: true, note: ("+31% VS 2025", "‎+31% مقارنة بـ2025"),
                showOnHome: true, homeOrder: 1),
            K("elite", 42, ("Elite athletes", "رياضيو النخبة"), 2,
                note: ("NATIONAL SQUAD POOL", "قاعدة المنتخب الوطني"), color: "swim",
                showOnHome: true, homeOrder: 2),
            K("participants", 18650, ("Race participations", "مشاركة في السباقات"), 3,
                showPlus: true, note: ("SINCE 2023", "منذ 2023"), color: "run",
                showOnHome: true, homeOrder: 4),
            K("tournaments", 24, ("Tournaments held", "بطولة أقيمت"), 4,
                note: ("ACROSS 9 REGIONS", "في 9 مناطق"), color: "bike",
                showOnHome: true, homeOrder: 3),
            K("community", 46, ("Community events", "فعالية مجتمعية"), 5),
            K("clubs", 17, ("Affiliated clubs", "نادياً منتسباً"), 6),
            K("regions", 9, ("Active regions", "مناطق نشطة"), 7),
            K("volunteers", 380, ("Trained volunteers", "متطوع مدرب"), 8),
            K("women", 31, ("Women participation", "مشاركة نسائية"), 9, suffix: "%"),
            K("youth", 27, ("Under-19 athletes", "رياضيون تحت 19"), 10, suffix: "%"),
        };

        RegionStat R(string key, (string En, string Ar) name, int athletes, int sortOrder) => new()
        {
            Key = key, NameEn = name.En, NameAr = name.Ar, Athletes = athletes, SortOrder = sortOrder,
        };

        var regions = new List<RegionStat>
        {
            R("riyadh", ("Riyadh", "الرياض"), 512, 1),
            R("makkah", ("Makkah (Jeddah)", "مكة المكرمة (جدة)"), 341, 2),
            R("eastern", ("Eastern Province", "المنطقة الشرقية"), 214, 3),
            R("madinah", ("Madinah / Yanbu", "المدينة / ينبع"), 96, 4),
            R("asir", ("Asir (Abha)", "عسير (أبها)"), 62, 5),
            R("tabuk", ("Tabuk / NEOM", "تبوك / نيوم"), 59, 6),
        };

        var growth = new List<GrowthPoint>
        {
            new() { Year = 2023, Athletes = 310 },
            new() { Year = 2024, Athletes = 640 },
            new() { Year = 2025, Athletes = 980 },
            new() { Year = 2026, Athletes = 1284 },
        };

        db.Kpis.AddRange(kpis);
        db.RegionStats.AddRange(regions);
        db.GrowthPoints.AddRange(growth);
        await db.SaveChangesAsync(ct);
    }
}
