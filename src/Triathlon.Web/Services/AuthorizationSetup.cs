using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Services;

/// <summary>Maps the three staff roles onto the capability policies the dashboard checks.</summary>
public static class AuthorizationSetup
{
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Content, policy => policy.RequireRole(Roles.SuperAdmin, Roles.Editor))
            .AddPolicy(Policies.Crm, policy => policy.RequireRole(Roles.SuperAdmin, Roles.CrmOfficer))
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.SuperAdmin));

        return services;
    }
}
