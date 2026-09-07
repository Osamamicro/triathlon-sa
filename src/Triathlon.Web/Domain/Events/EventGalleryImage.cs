using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Events;

/// <summary>One photo in an event's gallery. Owned by its <see cref="Event"/> — see ADR 0001.</summary>
public sealed class EventGalleryImage : BaseEntity
{
    public Guid EventId { get; set; }
    public required string Path { get; set; }
    public string? AltEn { get; set; }
    public string? AltAr { get; set; }
    public int SortOrder { get; set; }
}
