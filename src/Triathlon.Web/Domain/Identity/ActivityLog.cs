namespace Triathlon.Web.Domain.Identity;

/// <summary>
/// One append-only audit record: who changed what, when, and how. Deliberately not a
/// <see cref="Common.BaseEntity"/> — an audit trail that can be soft-deleted or edited is not an
/// audit trail, so it carries no stamp or soft-delete columns and is never updated after insert.
/// </summary>
public sealed class ActivityLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The signed-in user name, or <c>system</c> for jobs and startup work.</summary>
    public string User { get; set; } = string.Empty;

    /// <summary>The entity type touched, e.g. <c>Event</c>.</summary>
    public string Entity { get; set; } = string.Empty;

    /// <summary>The key of the row touched, as text so every key type fits.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>What happened, e.g. <c>create</c>, <c>update</c>, <c>publish</c>.</summary>
    public string Action { get; set; } = string.Empty;

    public DateTimeOffset At { get; set; }

    /// <summary>A JSON object of the form <c>{ "before": …, "after": … }</c>; null when nothing was captured.</summary>
    public string? Diff { get; set; }
}
