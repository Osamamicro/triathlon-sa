namespace Triathlon.Web.Domain.Events;

/// <summary>Lifecycle of one <see cref="EventRegistration"/>, as the CRM tracks it.</summary>
public enum RegistrationStatus
{
    Pending = 0,
    Confirmed = 1,
    Waitlist = 2,
    Cancelled = 3,
}
