using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// The structural seed every environment needs before the site is usable at all: the header/footer
/// navigation, the governance committees, the editable pages (<c>join.html</c>, <c>rules.html</c>,
/// <c>training.html</c>, <c>home</c>), the four site settings and the ten KPI tiles — started at
/// <c>Value = 0</c> here, so the statistics page and the home stat band render immediately rather
/// than 404ing on an empty table. <see cref="SeedStats"/> fills in the prototype's real numbers, but
/// only once (guarded by every KPI still reading zero), so it never overwrites a value the dashboard
/// or the nightly computed-KPI job has since written.
/// <para>
/// Switched on by <c>Database:SeedStructure</c>, default true: production runs this once at first
/// boot — with migrations already applied as their own deployment step — and every call after that
/// is a no-op because each of the five aggregates below guards its own table or its own keys.
/// <see cref="SeedContent"/> (the demo events/news/clubs/documents) is a separate switch entirely and
/// stays off in production.
/// </para>
/// </summary>
public static class SeedStructure
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<AppDbContext>();

        await SeedPages.NavigationAsync(db, ct);
        await SeedPages.CommitteesAsync(db, ct);
        await SeedPages.ContentPagesAsync(db, ct);
        await SettingsAsync(db, ct);
        await KpisAsync(db, ct);
    }

    // ---------------------------------------------------------------------------------------
    // Settings
    // ---------------------------------------------------------------------------------------

    /// <summary>Guarded per key, like <see cref="SeedPages.ContentPagesAsync"/>: a database seeded by an earlier task still picks up a setting added later.</summary>
    private static async Task SettingsAsync(AppDbContext db, CancellationToken ct)
    {
        var added = false;

        async Task SeedIfMissing(string key, string valueEn, string valueAr)
        {
            if (await db.SiteSettings.AnyAsync(s => s.Key == key, ct)) return;
            db.SiteSettings.Add(new SiteSetting { Key = key, ValueEn = valueEn, ValueAr = valueAr });
            added = true;
        }

        await SeedIfMissing(SettingKeys.ContactEmail, "info@triathlon.sa", "info@triathlon.sa");
        await SeedIfMissing(SettingKeys.ContactWebsite, "https://triathlon.sa", "https://triathlon.sa");
        await SeedIfMissing(SettingKeys.ContactX, "https://x.com/TriathlonKSA", "https://x.com/TriathlonKSA");
        await SeedIfMissing(SettingKeys.FooterBlurb,
            "The national governing body for triathlon, duathlon and aquathlon in the Kingdom of Saudi Arabia.",
            "الجهة الوطنية المنظمة لرياضات الترايثلون والدواثلون والأكواثلون في المملكة العربية السعودية.");

        if (added)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    // ---------------------------------------------------------------------------------------
    // KPIs (started at zero; SeedStats sets the prototype's real values)
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The same ten keys, labels, sort order and home-band flags <see cref="SeedStats"/> uses for the
    /// real numbers — guarded per key, like <see cref="SettingsAsync"/>, so a database that already
    /// has some KPI rows (say, one the dashboard added by hand) still picks up any still missing.
    /// </summary>
    private static async Task KpisAsync(AppDbContext db, CancellationToken ct)
    {
        var added = false;

        async Task SeedIfMissing(Kpi kpi)
        {
            if (await db.Kpis.AnyAsync(k => k.Key == kpi.Key, ct)) return;
            db.Kpis.Add(kpi);
            added = true;
        }

        Kpi K(string key, (string En, string Ar) label, int sortOrder,
            string? suffix = null, bool showPlus = false, (string En, string Ar)? note = null,
            bool showOnHome = false, int homeOrder = 0) => new()
        {
            Key = key, LabelEn = label.En, LabelAr = label.Ar, Value = 0, SortOrder = sortOrder,
            Suffix = suffix, ShowPlus = showPlus, NoteEn = note?.En, NoteAr = note?.Ar,
            ShowOnHome = showOnHome, HomeOrder = homeOrder, Source = KpiSource.Manual,
        };

        await SeedIfMissing(K("athletes", ("Registered athletes", "رياضي مسجل"), 1,
            showPlus: true, note: ("+31% VS 2025", "‎+31% مقارنة بـ2025"),
            showOnHome: true, homeOrder: 1));
        await SeedIfMissing(K("elite", ("Elite athletes", "رياضيو النخبة"), 2,
            note: ("NATIONAL SQUAD POOL", "قاعدة المنتخب الوطني"),
            showOnHome: true, homeOrder: 2));
        await SeedIfMissing(K("participants", ("Race participations", "مشاركة في السباقات"), 3,
            showPlus: true, note: ("SINCE 2023", "منذ 2023"),
            showOnHome: true, homeOrder: 4));
        await SeedIfMissing(K("tournaments", ("Tournaments held", "بطولة أقيمت"), 4,
            note: ("ACROSS 9 REGIONS", "في 9 مناطق"),
            showOnHome: true, homeOrder: 3));
        await SeedIfMissing(K("community", ("Community events", "فعالية مجتمعية"), 5));
        await SeedIfMissing(K("clubs", ("Affiliated clubs", "نادياً منتسباً"), 6));
        await SeedIfMissing(K("regions", ("Active regions", "مناطق نشطة"), 7));
        await SeedIfMissing(K("volunteers", ("Trained volunteers", "متطوع مدرب"), 8));
        await SeedIfMissing(K("women", ("Women participation", "مشاركة نسائية"), 9, suffix: "%"));
        await SeedIfMissing(K("youth", ("Under-19 athletes", "رياضيون تحت 19"), 10, suffix: "%"));

        if (added)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
