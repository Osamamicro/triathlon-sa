using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Triathlon.Web.Data;

namespace Triathlon.Web.Jobs;

/// <summary>
/// Background jobs, on the same database as everything else so a deployment is still one app and
/// one connection string. Hangfire creates and migrates its own schema on first run, which is why
/// nothing here appears in the EF migrations.
/// </summary>
public static class JobsSetup
{
    /// <summary>Where the job dashboard is mounted, inside the staff area.</summary>
    public const string DashboardPath = "/dashboard/jobs";

    public static IServiceCollection AddAppJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? DbSetup.Postgres;
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddHangfire(hangfire =>
        {
            hangfire
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (string.Equals(provider, DbSetup.Postgres, StringComparison.OrdinalIgnoreCase))
            {
                hangfire.UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(connectionString));
            }
            else if (string.Equals(provider, DbSetup.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                hangfire.UseSqlServerStorage(connectionString);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown database provider '{provider}'. Set Database:Provider to "
                    + $"'{DbSetup.Postgres}' or '{DbSetup.SqlServer}'.");
            }
        });

        services.AddHangfireServer();

        return services;
    }

    /// <summary>
    /// Mounts the job dashboard. It is an ordinary route under <c>/dashboard</c>, but it is served
    /// by Hangfire rather than Blazor, so it carries its own authorisation filter.
    /// </summary>
    public static IEndpointConventionBuilder MapAppJobsDashboard(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHangfireDashboard(DashboardPath, new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorization()],

            // The dashboard can trigger, requeue and delete jobs. Read-only would be safer, but the
            // point of exposing it to the administrator is to be able to retry a failed mail run.
            IsReadOnlyFunc = _ => false,
        });
}
