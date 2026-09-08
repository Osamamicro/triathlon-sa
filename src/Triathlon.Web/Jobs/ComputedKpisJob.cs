using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Crm;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Domain.Stats;
using Triathlon.Web.Services;

namespace Triathlon.Web.Jobs;

/// <summary>
/// The nightly recompute of every KPI whose <see cref="Kpi.Source"/> is <see cref="KpiSource.Computed"/>,
/// scheduled by <c>JobsSetup.MapAppJobsDashboard</c> at 02:00 Riyadh (23:00 UTC — Saudi Arabia keeps
/// no DST). A manual KPI is never touched here; only the handful the federation has flagged as
/// computed from the live tables are recomputed, and only when the recomputed value actually moved.
/// </summary>
public sealed class ComputedKpisJob(AppDbContext db, TimeProvider clock, ContentCommit commit, ILogger<ComputedKpisJob> log)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(EventsService.RiyadhOffset).DateTime);

        var values = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["athletes"] = await db.Athletes.CountAsync(a => a.Status == AthleteStatus.Approved, ct),
            ["participants"] = await db.EventRegistrations.CountAsync(r => r.Status == RegistrationStatus.Confirmed, ct),
            ["tournaments"] = await db.Events.CountAsync(e => e.IsPublished && e.Type == EventType.Competition && e.DateStart < today, ct),
            ["community"] = await db.Events.CountAsync(e => e.IsPublished && e.Type == EventType.Community && e.DateStart < today, ct),
            ["clubs"] = await db.Clubs.CountAsync(c => c.IsActive, ct),
        };

        var computed = await db.Kpis.Where(k => k.Source == KpiSource.Computed).ToListAsync(ct);

        var before = new Dictionary<string, long>(StringComparer.Ordinal);
        var after = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var kpi in computed)
        {
            if (!values.TryGetValue(kpi.Key, out var value))
            {
                log.LogWarning("Computed KPI {Key} has no known source query; skipped.", kpi.Key);
                continue;
            }

            if (kpi.Value == value)
            {
                continue;
            }

            before[kpi.Key] = kpi.Value;
            kpi.Value = value;
            after[kpi.Key] = value;
        }

        if (before.Count > 0)
        {
            await commit.ApplyAsync("Kpi", Guid.Empty, "compute", before, after, [CacheTags.Stats, CacheTags.Home], ct);
        }
    }
}
