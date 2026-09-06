using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Triathlon.Web.Data;

/// <summary>
/// Design-time factory for the SQL Server migration set. It takes the connection string from configuration so
/// that <c>dotnet ef database update</c> hits the database the environment is actually pointed at, and only
/// falls back to the local developer default when nothing is configured, so <c>migrations add</c> still needs
/// no live server.
/// </summary>
public sealed class SqlServerContextFactory : IDesignTimeDbContextFactory<SqlServerDbContext>
{
    private const string LocalFallback = "Server=localhost;Database=triathlon;Trusted_Connection=True;TrustServerCertificate=True";

    public SqlServerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqlServerDbContext>()
            .UseSqlServer(DesignTimeConfiguration.ConnectionStringOrFallback(LocalFallback))
            .Options;

        return new SqlServerDbContext(options);
    }
}
