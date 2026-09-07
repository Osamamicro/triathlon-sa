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
///
/// Both provider contexts are registered unconditionally and <see cref="AppDbContext"/> is a scoped
/// forward to whichever one the configuration names, so the provider — like the connection string —
/// is read from the built host rather than from the configuration handed to registration. Nothing of
/// the unused provider is ever constructed: its options are built by a callback that only runs if its
/// context is asked for.
/// </summary>
public static class DbSetup
{
    public const string Postgres = "Postgres";
    public const string SqlServer = "SqlServer";

    /// <remarks>
    /// <paramref name="configuration"/> is deliberately not read here; see the type's summary. The
    /// parameter stays so every <c>AddApp…</c> call reads the same at the call site.
    /// </remarks>
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);

        // Both scoped, and for the same reason: the acting user is a property of the request or the
        // circuit the save is running on, so the interceptor has to be built per scope with that
        // scope's user. The options callbacks below resolve it from the scope that creates the context.
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        services.TryAddScoped(sp => new StampInterceptor(
            sp.GetRequiredService<TimeProvider>(),
            sp.GetService<ICurrentUser>()));

        services.AddDbContext<PostgresDbContext>((sp, options) => options
            .UseNpgsql(ConnectionString(sp), npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory"))
            .AddInterceptors(sp.GetRequiredService<StampInterceptor>()));

        services.AddDbContext<SqlServerDbContext>((sp, options) => options
            .UseSqlServer(ConnectionString(sp), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory"))
            .AddInterceptors(sp.GetRequiredService<StampInterceptor>()));

        // Everything else — Identity's stores, the health check, the activity logger, the services and
        // the startup migration — asks for AppDbContext, so this one line decides the provider for all
        // of them, and EF still matches each migration set by concrete context type.
        services.AddScoped<AppDbContext>(Context);

        services.AddDatabaseDeveloperPageExceptionFilter();

        return services;
    }

    /// <summary>
    /// Picks the provider-specific context for the scope being served. Reading the configuration here
    /// rather than at registration is what lets a test host — or any configuration source layered in
    /// after <c>AddAppDatabase</c> was called — choose the provider; a value captured at registration
    /// would pin every context to the settings file while the connection string came from elsewhere.
    /// </summary>
    private static AppDbContext Context(IServiceProvider services)
    {
        var provider = services.GetRequiredService<IConfiguration>()["Database:Provider"] ?? Postgres;

        if (string.Equals(provider, Postgres, StringComparison.OrdinalIgnoreCase))
        {
            return services.GetRequiredService<PostgresDbContext>();
        }

        if (string.Equals(provider, SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            return services.GetRequiredService<SqlServerDbContext>();
        }

        throw new InvalidOperationException(
            $"Unknown database provider '{provider}'. Set Database:Provider to '{Postgres}' or '{SqlServer}'.");
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
