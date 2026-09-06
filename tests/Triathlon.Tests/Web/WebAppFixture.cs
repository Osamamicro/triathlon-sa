using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Triathlon.Tests.Web;

/// <summary>
/// Boots the real application against a throwaway PostgreSQL container, with startup migration and
/// identity seeding switched on, so the tests exercise the same wiring production uses.
/// </summary>
public sealed class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "seed-admin@triathlon.test";
    public const string AdminPassword = "Seed-Admin-Pass-2026!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Touching Services builds the host, which is what migrates and seeds.
        _ = Services;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Database:Provider"] = "Postgres",
                ["Database:MigrateOnStartup"] = "true",
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
            }));
    }
}

/// <summary>Shares one container and one application host across every web test class.</summary>
[CollectionDefinition(Name)]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "web-app";
}
