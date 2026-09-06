using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Triathlon.Web.Data;

/// <summary>Design-time factory so <c>dotnet ef</c> can build the SQL Server model without a live server.</summary>
public sealed class SqlServerContextFactory : IDesignTimeDbContextFactory<SqlServerDbContext>
{
    public SqlServerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqlServerDbContext>()
            .UseSqlServer("Server=localhost;Database=triathlon;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new SqlServerDbContext(options);
    }
}
