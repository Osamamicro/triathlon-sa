using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Services;

/// <summary>
/// The read side of the audit trail: what <see cref="ActivityDrawer"/>'s (see the Dashboard
/// components) and any future "recent activity" screen query, newest first. <see cref="ActivityLog"/>
/// itself is append-only and has no dedicated write service — every write goes through
/// <see cref="ActivityLogger"/> or <see cref="ContentCommit"/> alongside the change it is logging.
/// </summary>
public sealed class ActivityQuery(AppDbContext db)
{
    /// <summary>
    /// Every logged action against one entity type, optionally narrowed to a single row, newest
    /// first. <paramref name="id"/> is compared as text — <see cref="ActivityLog.EntityId"/> is a
    /// string so every key type (Guid, int, composite) fits the same column.
    /// </summary>
    public async Task<IReadOnlyList<ActivityLog>> ForEntityAsync(string entity, Guid? id, int take, CancellationToken ct)
    {
        var q = db.ActivityLogs.AsNoTracking().Where(a => a.Entity == entity);
        if (id is { } value)
        {
            var text = value.ToString();
            q = q.Where(a => a.EntityId == text);
        }

        return await q.OrderByDescending(a => a.At).Take(Math.Clamp(take, 1, 200)).ToListAsync(ct);
    }

    /// <summary>The most recent activity across every entity, newest first — a dashboard-wide feed.</summary>
    public async Task<IReadOnlyList<ActivityLog>> LatestAsync(int take, CancellationToken ct) =>
        await db.ActivityLogs.AsNoTracking().OrderByDescending(a => a.At).Take(Math.Clamp(take, 1, 200)).ToListAsync(ct);
}
