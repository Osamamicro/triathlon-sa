using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Events;

/// <summary>
/// A guest entry into an event, posted from the public site's uncached registration form. A CRM
/// record of a person, not owned by the event — see ADR 0001: it is never cascaded when the event
/// is soft-deleted.
/// </summary>
public sealed class EventRegistration : BaseEntity
{
    public Guid EventId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Category { get; set; }
    public string? Club { get; set; }
    public RegistrationStatus Status { get; set; }
}
