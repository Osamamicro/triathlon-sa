namespace Triathlon.Web;

/// <summary>
/// Marker type for the public site's shared resources (<c>Resources/Shared.*.resx</c>): inject
/// <c>IStringLocalizer&lt;Shared&gt;</c> to reach them.
/// </summary>
/// <remarks>
/// It deliberately sits in the assembly's root namespace even though the file lives under
/// <c>Resources/</c>. <c>ResourceManagerStringLocalizerFactory</c> builds the resource prefix as
/// "root namespace" + <c>ResourcesPath</c> + "type name minus the root namespace", so a type in
/// <c>Triathlon.Web.Resources</c> would look for <c>Resources/Resources.Shared.resx</c>.
/// </remarks>
public sealed class Shared;
