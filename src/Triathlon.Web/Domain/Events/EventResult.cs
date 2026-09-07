using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Events;

/// <summary>One finisher row on an event's results page. Owned by its <see cref="Event"/> — see ADR 0001.</summary>
public sealed class EventResult : BaseEntity
{
    public Guid EventId { get; set; }
    public int Position { get; set; }
    public required string AthleteEn { get; set; }
    public required string AthleteAr { get; set; }
    public string? ClubEn { get; set; }
    public string? ClubAr { get; set; }

    /// <summary>Finish time as printed, e.g. <c>"58:41"</c> or <c>"1:00:26"</c> — never parsed, only displayed.</summary>
    public required string Time { get; set; }
}
