using Hangfire.Dashboard;
using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Jobs;

/// <summary>
/// Gates Hangfire's own dashboard.
/// <para>
/// Hangfire ships with no authorisation of its own and will happily serve its job list — including
/// arguments, which for this application means athlete email addresses — to anyone who can reach
/// the URL. This restricts it to <see cref="Roles.SuperAdmin"/>, the role behind
/// <see cref="Policies.Admin"/>. The role is checked directly rather than through
/// <c>IAuthorizationService</c> because Hangfire's filter contract is synchronous, and the policy is
/// a single <c>RequireRole</c> — see <c>Services/AuthorizationSetup.cs</c>. Requests arrive here
/// after <c>UseAuthentication</c>, so the identity cookie has already been turned into a principal.
/// </para>
/// </summary>
public sealed class HangfireDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetHttpContext().User.IsInRole(Roles.SuperAdmin);
    }
}
