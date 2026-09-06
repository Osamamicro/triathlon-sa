using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Triathlon.Web.Data;

/// <summary>Design-time factory so <c>dotnet ef</c> can build the PostgreSQL model without a live server.</summary>
public sealed class PostgresContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
    public PostgresDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=triathlon;Username=postgres;Password=postgres")
            .Options;

        return new PostgresDbContext(options);
    }
}
