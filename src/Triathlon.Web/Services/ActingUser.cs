namespace Triathlon.Web.Services;

/// <summary>
/// Carries the acting user's name across a <c>DashboardScope.RunAsync</c> call, from the circuit or
/// request scope that resolved <c>DashboardScope</c> into the fresh child scope it creates for one
/// read-for-edit or save.
/// <para>
/// A dashboard page's own scope has a live <see cref="ICurrentUser"/> (the circuit's authentication
/// state, or the request's <c>HttpContext</c>), but <c>DashboardScope.RunAsync</c> deliberately runs
/// the work in a brand-new child scope so a refused save never leaves a dirty <c>AppDbContext</c>
/// behind. That fresh scope gets its own, unrelated <c>AuthenticationStateProvider</c> — a Blazor
/// circuit's authentication state lives on the circuit's own scope, not on any scope created off of
/// it — so without this marker the child scope's <see cref="CurrentUser"/> could only ever fall back
/// to <c>IHttpContextAccessor</c>, which a circuit does not reliably have either. <c>DashboardScope</c>
/// reads the acting user's name from the circuit's <see cref="ICurrentUser"/> before creating the
/// child scope, and stamps it here so the child scope's own <see cref="CurrentUser"/> can answer with
/// it instead of re-deriving (and failing to re-derive) the same answer from scratch.
/// </para>
/// </summary>
public sealed class ActingUser
{
    public string? Name { get; set; }
}
