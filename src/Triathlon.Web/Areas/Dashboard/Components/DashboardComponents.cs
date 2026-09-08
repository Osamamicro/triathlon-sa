using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Areas.Dashboard.Components;

/// <summary>
/// One changed field between an <see cref="ActivityLog"/>'s before and after snapshot — a key that
/// carried different values on the two sides. <see cref="ActivityDrawer"/> only ever shows rows of
/// this shape, never the full snapshot, since most of a save's fields do not change on a given edit.
/// </summary>
public sealed record DiffRow(string Key, string? Before, string? After);

/// <summary>One activity-log entry paired with the changed keys parsed out of its diff.</summary>
public sealed record ActivityRow(ActivityLog Log, IReadOnlyList<DiffRow> Changes);
