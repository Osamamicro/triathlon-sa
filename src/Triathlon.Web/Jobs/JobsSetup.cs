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

    /// <remarks>
    /// The storage is resolved from the built host's configuration rather than from the
    /// <paramref name="configuration"/> captured here, exactly as <see cref="DbSetup"/> resolves the
    /// EF connection string: registration runs before a test host or a late configuration source has
    /// been layered in, and a connection string read now would pin the job storage to the settings
    /// file while the rest of the application talks to somewhere else. The parameter stays so every
    /// <c>AddApp…</c> call reads the same at the call site.
    /// </remarks>
    public static IServiceCollection AddAppJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire((serviceProvider, hangfire) =>
        {
            var settings = serviceProvider.GetRequiredService<IConfiguration>();
            var provider = settings["Database:Provider"] ?? DbSetup.Postgres;
            var connectionString = settings.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            // Chosen before anything is applied, so an unknown provider fails without leaving a
            // half-configured Hangfire behind.
            Action<IGlobalConfiguration> useStorage;

            if (string.Equals(provider, DbSetup.Postgres, StringComparison.OrdinalIgnoreCase))
            {
                useStorage = config => config.UsePostgreSqlStorage(
                    storage => storage.UseNpgsqlConnection(connectionString));
            }
            else if (string.Equals(provider, DbSetup.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                useStorage = config => config.UseSqlServerStorage(connectionString);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown database provider '{provider}'. Set Database:Provider to "
                    + $"'{DbSetup.Postgres}' or '{DbSetup.SqlServer}'.");
            }

            hangfire
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            useStorage(hangfire);
        });

        services.AddHangfireServer();

        return services;
    }

    /// <summary>
    /// Mounts the job dashboard and the app's one recurring job. The dashboard is an ordinary route
    /// under <c>/dashboard</c>, but it is served by Hangfire rather than Blazor, so it carries its
    /// own authorisation filter.
    /// </summary>
    public static IEndpointConventionBuilder MapAppJobsDashboard(this IEndpointRouteBuilder endpoints)
    {
        var dashboard = endpoints.MapHangfireDashboard(DashboardPath, new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorization()],

            // The dashboard can trigger, requeue and delete jobs. Read-only would be safer, but the
            // point of exposing it to the administrator is to be able to retry a failed mail run.
            IsReadOnlyFunc = _ => false,
        });

        // 02:00 Riyadh, which is 23:00 UTC the previous day — Saudi Arabia keeps no DST, so this
        // cron expression never needs a seasonal adjustment.
        endpoints.ServiceProvider.GetRequiredService<IRecurringJobManager>()
            .AddOrUpdate<ComputedKpisJob>("computed-kpis", job => job.RunAsync(CancellationToken.None), "0 23 * * *");

        return dashboard;
    }
}
