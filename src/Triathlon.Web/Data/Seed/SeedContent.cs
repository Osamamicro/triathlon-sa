using Triathlon.Web.Data;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Loads the prototype's demonstration content into an empty database so a developer, the tests and
/// the staging site have something to look at. Each seeder is idempotent (skips when its table has
/// rows). Switched on by <c>Database:SeedContent</c>; production leaves it off and loads real
/// content through the dashboard. Assumes <see cref="SeedStructure"/> has already run — that is
/// where the navigation, committees, pages, settings and the ten zero-valued KPI rows come from;
/// this seeder only adds the demonstration events, documents, clubs, news and the KPIs' real values.
/// </summary>
public static class SeedContent
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await SeedEvents.RunAsync(db, ct);
        await SeedDocuments.RunAsync(db, ct);
        await SeedClubs.RunAsync(db, ct);
        await SeedNews.RunAsync(db, ct);
        await SeedStats.RunAsync(db, ct);
    }
}
