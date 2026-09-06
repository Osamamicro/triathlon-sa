using Microsoft.AspNetCore.Identity;

namespace Triathlon.Web.Domain.Identity;

/// <summary>
/// A staff account for the dashboard. Accounts are created by an administrator, never self-registered,
/// so the only profile data carried here is the name shown in the UI and the audit trail.
/// </summary>
public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
