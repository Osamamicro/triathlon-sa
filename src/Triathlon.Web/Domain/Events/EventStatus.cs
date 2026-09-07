namespace Triathlon.Web.Domain.Events;

/// <summary>
/// Derived, not stored — see <see cref="Event.StatusOn"/>. Draft/publish is a separate concern
/// (<see cref="Event.IsPublished"/>).
/// </summary>
public enum EventStatus
{
    Open,
    OpensSoon,
    Completed,
}
