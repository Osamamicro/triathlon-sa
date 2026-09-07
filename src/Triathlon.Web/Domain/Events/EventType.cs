namespace Triathlon.Web.Domain.Events;

/// <summary>Whether an event is a race with results and categories, or a community ride/swim/run.</summary>
public enum EventType
{
    Competition = 1,
    Community = 2,
}
