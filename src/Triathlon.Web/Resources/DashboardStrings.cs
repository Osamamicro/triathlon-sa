namespace Triathlon.Web;

/// <summary>
/// Marker type for the dashboard's shared resources (<c>Resources/DashboardStrings.*.resx</c>):
/// inject <c>IStringLocalizer&lt;DashboardStrings&gt;</c> to reach them.
/// </summary>
/// <remarks>
/// Named <c>DashboardStrings</c> rather than <c>Dashboard</c> because a type named <c>Dashboard</c>
/// in the assembly's root namespace would collide with the <c>Triathlon.Web.Areas.Dashboard</c>
/// namespace. It sits in the root namespace anyway, for the same reason <see cref="Shared"/> does:
/// <c>ResourceManagerStringLocalizerFactory</c> builds the resource prefix as "root namespace" +
/// <c>ResourcesPath</c> + "type name minus the root namespace", so a type under
/// <c>Triathlon.Web.Resources</c> would look for <c>Resources/Resources.DashboardStrings.resx</c>.
/// </remarks>
public sealed class DashboardStrings;
