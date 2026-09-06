using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp => new StampInterceptor(
            sp.GetRequiredService<TimeProvider>(),
            sp.GetService<IHttpContextAccessor>()));

        if (string.Equals(provider, Postgres, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, PostgresDbContext>((sp, options) => options
                .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory"))
                .AddInterceptors(sp.GetRequiredService<StampInterceptor>()));
        }
        else if (string.Equals(provider, SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext, SqlServerDbContext>((sp, options) => options
                .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory"))
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
}
