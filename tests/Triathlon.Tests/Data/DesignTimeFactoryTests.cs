using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;

namespace Triathlon.Tests.Data;

/// <summary>
/// EF tooling prefers a design-time factory over the application host, so these factories are what
/// <c>dotnet ef database update</c> actually talks through. They must obey configuration rather than a
/// hardcoded localhost string, or a staging/production update would silently hit the wrong database.
/// </summary>
public sealed class DesignTimeFactoryTests
{
    private const string Configured = "Host=configured-host;Port=6432;Database=configured;Username=u;Password=p";

    [Fact]
    public void Postgres_factory_uses_the_configured_connection_string()
    {
        using var connectionString = new EnvironmentVariable("ConnectionStrings__Default", Configured);

        using var context = new PostgresContextFactory().CreateDbContext([]);

        Assert.Equal(Configured, context.Database.GetConnectionString());
    }

    [Fact]
    public void SqlServer_factory_uses_the_configured_connection_string()
    {
        const string configured = "Server=configured-host;Database=configured;User Id=u;Password=p;TrustServerCertificate=True";
        using var connectionString = new EnvironmentVariable("ConnectionStrings__Default", configured);

        using var context = new SqlServerContextFactory().CreateDbContext([]);

        // Microsoft.Data.SqlClient normalises the keywords ("Server" becomes "Data Source"), so assert on
        // the target rather than the literal string: it is the configured server, not the local fallback.
        var actual = context.Database.GetConnectionString();
        Assert.Contains("configured-host", actual, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost", actual, StringComparison.Ordinal);
    }

    [Fact]
    public void Falls_back_to_the_local_default_when_nothing_is_configured()
    {
        using var connectionString = new EnvironmentVariable("ConnectionStrings__Default", null);
        var settingsFreeDirectory = Directory.CreateTempSubdirectory("stf-designtime");

        try
        {
            Assert.Equal(
                "the-fallback",
                DesignTimeConfiguration.ConnectionStringOrFallback("the-fallback", settingsFreeDirectory.FullName));
        }
        finally
        {
            settingsFreeDirectory.Delete(recursive: true);
        }
    }

    /// <summary>Sets an environment variable for the duration of a test and restores whatever was there.</summary>
    private sealed class EnvironmentVariable : IDisposable
    {
        private readonly string _name;
        private readonly string? _original;

        public EnvironmentVariable(string name, string? value)
        {
            _name = name;
            _original = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_name, _original);
    }
}
