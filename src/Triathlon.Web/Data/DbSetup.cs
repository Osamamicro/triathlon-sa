using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Triathlon.Web.Services;

namespace Triathlon.Web.Data;

/// <summary>
/// Database wiring for the application. The provider is chosen by configuration
/// (<c>Database:Provider</c> = <c>Postgres</c> by default, or <c>SqlServer</c>) and each provider keeps
/// its own migration set, regenerated from <c>src/Triathlon.Web</c> with:
///
///   dotnet ef migrations add &lt;Name&gt; --context PostgresDbContext  -o Data/Migrations/Postgres
///   dotnet ef migrations add &lt;Name&gt; --context SqlServerDbContext -o Data/Migrations/SqlServer
/// </summary>
public static class DbSetup
{
    public const string Postgres = "Postgres";
    public const string SqlServer = "SqlServer";

    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? Postgres;

        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);

        // Both scoped, and for the same reason: the acting user is a property of the request or the
        // circuit the save is running on, so the interceptor has to be built per scope with that
        // scope's user. The options callbacks below resolve it from the scope that creates the context.
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        services.TryAddScoped(sp => new StampInterceptor(
            sp.GetRequiredService<TimeProvider>(),
            sp.GetService<ICurrentUser>()));

        if (string.Equals(provider, Postgres, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, PostgresDbContext>((sp, options) => options
                .UseNpgsql(ConnectionString(sp), npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory"))
                .AddInterceptors(sp.GetRequiredService<StampInterceptor>()));
        }
        else if (string.Equals(provider, SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, SqlServerDbContext>((sp, options) => options
                .UseSqlServer(ConnectionString(sp), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory"))
                .AddInterceptors(sp.GetRequiredService<StampInterceptor>()));
        }
        else
        {
            throw new InvalidOperationException(
                $"Unknown database provider '{provider}'. Set Database:Provider to '{Postgres}' or '{SqlServer}'.");
        }

        services.AddDatabaseDeveloperPageExceptionFilter();

        return services;
    }

    /// <summary>
    /// Resolved when the first context is created rather than when the service is registered, so the
    /// connection string is whatever the built host's configuration says. Registration runs before a
    /// test host or a late configuration source has been layered in, and a string captured there would
    /// quietly pin every context to the settings file.
    /// </summary>
    private static string ConnectionString(IServiceProvider services) =>
        services.GetRequiredService<IConfiguration>().GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");
}
