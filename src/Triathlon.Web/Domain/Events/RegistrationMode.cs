namespace Triathlon.Web.Domain.Events;

/// <summary>
/// How an event accepts entrants: not at all, through the site's own guest-entry form, or through a
/// third-party registration link.
/// </summary>
public enum RegistrationMode
{
    None = 0,
    Internal = 1,
    External = 2,
}
