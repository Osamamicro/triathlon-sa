using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Services;

/// <summary>
/// The read side of the audit trail, extracted from <see cref="ActivityQuery"/> so
/// <see cref="Areas.Dashboard.Components.ActivityDrawer"/> can be rendered in a bUnit test against a
/// stub instead of a real <see cref="Data.AppDbContext"/>.
/// </summary>
public interface IActivityQuery
{
    /// <summary>
    /// Every logged action against one entity type, optionally narrowed to a single row, newest
    /// first. <paramref name="id"/> is compared as text — <see cref="ActivityLog.EntityId"/> is a
    /// string so every key type (Guid, int, composite) fits the same column.
    /// </summary>
    Task<IReadOnlyList<ActivityLog>> ForEntityAsync(string entity, Guid? id, int take, CancellationToken ct);

    /// <summary>The most recent activity across every entity, newest first — a dashboard-wide feed.</summary>
    Task<IReadOnlyList<ActivityLog>> LatestAsync(int take, CancellationToken ct);
}
