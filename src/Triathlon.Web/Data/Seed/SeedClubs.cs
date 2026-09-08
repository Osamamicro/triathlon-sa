using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Crm;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// The prototype's six affiliated clubs (<c>assets/js/data.js</c>). Idempotent on its own table, so
/// a database seeded by an earlier task picks the clubs up on the next start.
/// </summary>
public static class SeedClubs
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Clubs.IgnoreQueryFilters().AnyAsync(ct)) return;

        Club C((string En, string Ar) name, (string En, string Ar) city, int sortOrder) => new()
        {
            NameEn = name.En, NameAr = name.Ar, CityEn = city.En, CityAr = city.Ar,
            IsActive = true, SortOrder = sortOrder,
        };

        db.Clubs.AddRange(
            C(("Riyadh Tri Club", "نادي الرياض للترايثلون"), ("Riyadh", "الرياض"), 1),
            C(("Jeddah Waves", "أمواج جدة"), ("Jeddah", "جدة"), 2),
            C(("Eastern Endurance", "تحمّل الشرقية"), ("Dammam / Khobar", "الدمام / الخبر"), 3),
            C(("Yanbu Open Water", "ينبع للمياه المفتوحة"), ("Yanbu", "ينبع"), 4),
            C(("Asir Peaks", "قمم عسير"), ("Abha", "أبها"), 5),
            C(("NEOM Multisport", "نيوم للرياضات المتعددة"), ("NEOM / Tabuk", "نيوم / تبوك"), 6));

        await db.SaveChangesAsync(ct);
    }
}
