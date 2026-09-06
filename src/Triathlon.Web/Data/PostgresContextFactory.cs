using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Triathlon.Web.Data;

/// <summary>
/// Design-time factory for the PostgreSQL migration set. It takes the connection string from configuration so
/// that <c>dotnet ef database update</c> hits the database the environment is actually pointed at, and only
/// falls back to the local developer default when nothing is configured, so <c>migrations add</c> still needs
/// no live server.
/// </summary>
public sealed class PostgresContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
    private const string LocalFallback = "Host=localhost;Port=5432;Database=triathlon;Username=postgres;Password=postgres";

    public PostgresDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql(DesignTimeConfiguration.ConnectionStringOrFallback(LocalFallback))
            .Options;

        return new PostgresDbContext(options);
    }
}
