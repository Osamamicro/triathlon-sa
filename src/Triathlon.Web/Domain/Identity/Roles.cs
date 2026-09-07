namespace Triathlon.Web.Domain.Identity;

/// <summary>The three staff roles. Membership is granted by a <see cref="Roles.SuperAdmin"/>.</summary>
public static class Roles
{
    /// <summary>Full access, including user management and settings.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Edits and publishes site content.</summary>
    public const string Editor = "Editor";

    /// <summary>Works the CRM: athletes, clubs, registrations.</summary>
    public const string CrmOfficer = "CrmOfficer";

    public static readonly string[] All = [SuperAdmin, Editor, CrmOfficer];
}

/// <summary>
/// Authorisation policy names. Screens and endpoints ask for a policy, never a role, so the role
/// mix behind a capability can change in one place.
/// </summary>
public static class Policies
{
    /// <summary>Content management: <see cref="Roles.SuperAdmin"/> or <see cref="Roles.Editor"/>.</summary>
    public const string Content = "Content";

    /// <summary>CRM: <see cref="Roles.SuperAdmin"/> or <see cref="Roles.CrmOfficer"/>.</summary>
    public const string Crm = "Crm";

    /// <summary>Administration: <see cref="Roles.SuperAdmin"/> only.</summary>
    public const string Admin = "Admin";
}
