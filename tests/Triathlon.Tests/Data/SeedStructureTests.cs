using System.Data.Common;
using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;

namespace Triathlon.Tests.Data;

/// <summary>
/// <c>SeedStructure</c> — navigation, committees, editable pages, site settings and the ten
/// zero-valued KPI rows — has to run on its own, without <c>SeedContent</c>'s demo events, news and
/// clubs: that is the shape a first production boot takes, where migrations already ran as their own
/// deployment step and the CMS still needs to be usable the moment the app comes up.
/// <para>
/// This points a fresh application host at a brand-new, empty database on the same server the shared
/// fixture's container already runs (created here with a raw admin connection, since neither
/// provider's EF migrations can create their own database), with <c>Database:SeedContent = false</c>
/// and <c>Database:SeedStructure = true</c>, and checks the structural rows land while the demo
/// content does not.
/// </para>
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class SeedStructureTests(WebAppFixture app)
{
    [Fact]
    public async Task Structural_seed_runs_without_demo_content()
    {
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var baseConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        var isSqlServer = string.Equals(configuration["Database:Provider"], DbSetup.SqlServer, StringComparison.OrdinalIgnoreCase);

        var databaseName = "structure_only_" + Guid.NewGuid().ToString("N");
        var (adminConnectionString, targetConnectionString) = isSqlServer
            ? SqlServerConnectionStrings(baseConnectionString, databaseName)
            : PostgresConnectionStrings(baseConnectionString, databaseName);

        await CreateDatabaseAsync(isSqlServer, adminConnectionString, databaseName);
        try
        {
            using var host = app.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = targetConnectionString,
                    ["Database:SeedContent"] = "false",
                    ["Database:SeedStructure"] = "true",
                    ["Database:MigrateOnStartup"] = "true",
                })));

            using var client = host.CreateClient();

            using var home = await client.GetAsync("/en");
            Assert.Equal(HttpStatusCode.OK, home.StatusCode);
            var homeHtml = await home.Content.ReadAsStringAsync();
            Assert.Contains(">Events</a>", homeHtml, StringComparison.Ordinal);       // navigation
            Assert.Contains("info@triathlon.sa", homeHtml, StringComparison.Ordinal); // settings, via the footer

            using var governance = await client.GetAsync("/en/governance");
            Assert.Equal(HttpStatusCode.OK, governance.StatusCode);
            var governanceHtml = await governance.Content.ReadAsStringAsync();
            Assert.Contains("Board of Directors", governanceHtml, StringComparison.Ordinal); // committees

            using var eventsPage = await client.GetAsync("/en/events");
            Assert.Equal(HttpStatusCode.OK, eventsPage.StatusCode);
            var eventsHtml = await eventsPage.Content.ReadAsStringAsync();
            Assert.Contains("No events match this filter.", eventsHtml, StringComparison.Ordinal); // no demo events

            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var kpis = await db.Kpis.ToListAsync();

            Assert.Equal(10, kpis.Count);
            Assert.All(kpis, k => Assert.Equal(0, k.Value));
        }
        finally
        {
            await DropDatabaseAsync(isSqlServer, adminConnectionString, databaseName);
        }
    }

    private static (string Admin, string Target) PostgresConnectionStrings(string baseConnectionString, string databaseName)
    {
        // Every other setting (host, port, credentials) is carried over unchanged from the fixture's
        // own connection string — only Database differs between the two.
        var admin = new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = "postgres" }.ConnectionString;
        var target = new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = databaseName }.ConnectionString;

        return (admin, target);
    }

    private static (string Admin, string Target) SqlServerConnectionStrings(string baseConnectionString, string databaseName)
    {
        var admin = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" }.ConnectionString;
        var target = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = databaseName }.ConnectionString;

        return (admin, target);
    }

    // CREATE/DROP DATABASE cannot parameterize an identifier the way a data value can — neither
    // provider accepts "CREATE DATABASE @name" — so the name is interpolated below rather than
    // bound. That is safe here: databaseName is this method's own Guid.ToString("N"), never
    // visitor-supplied text.
#pragma warning disable DAP241 // Data values should not be interpolated into SQL string

    private static async Task CreateDatabaseAsync(bool isSqlServer, string adminConnectionString, string databaseName)
    {
        if (isSqlServer)
        {
            await using var connection = new SqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
        }
        else
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>Best effort: a leftover throwaway database on the shared test container is not worth failing the run for.</summary>
    private static async Task DropDatabaseAsync(bool isSqlServer, string adminConnectionString, string databaseName)
    {
        try
        {
            if (isSqlServer)
            {
                await using var connection = new SqlConnection(adminConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                // Single-user with rollback immediate kicks out any pooled connections still
                // referencing the database (the target host's own pool), which would otherwise
                // block the drop.
                command.CommandText =
                    $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
            }
            else
            {
                await using var connection = new NpgsqlConnection(adminConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (DbException)
        {
            // Best effort — see summary.
        }
    }

#pragma warning restore DAP241
}
