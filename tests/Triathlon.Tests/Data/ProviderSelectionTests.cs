using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Triathlon.Web.Data;
using Triathlon.Web.Jobs;

namespace Triathlon.Tests.Data;

/// <summary>
/// One configuration key picks the database provider for both EF and Hangfire, and both read it from
/// the built host rather than from what registration was handed. A typo in it must stop the
/// application with a sentence that says what to do, not start a site that silently cannot reach its
/// data — and a host that says SQL Server must get the SQL Server context, however the application
/// was registered.
/// </summary>
public sealed class ProviderSelectionTests
{
    private const string Unknown = "Oracle";

    private const string PostgresConnection = "Host=nowhere;Database=triathlon;Username=u;Password=p";

    private const string SqlServerConnection =
        "Server=nowhere;Database=triathlon;User Id=u;Password=p;TrustServerCertificate=True";

    [Fact]
    public void An_unknown_provider_is_refused_when_the_database_context_is_resolved()
    {
        // DbSetup reads the configuration lazily, so the mistake surfaces on first resolve instead of
        // at registration — the same shape as JobsSetup below.
        using var provider = Provider(Configuration(Unknown, PostgresConnection), Configuration(Unknown, PostgresConnection));
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<AppDbContext>());

        Assert.Contains(Unknown, exception.Message, StringComparison.Ordinal);
        Assert.Contains(DbSetup.Postgres, exception.Message, StringComparison.Ordinal);
        Assert.Contains(DbSetup.SqlServer, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DbSetup.Postgres, PostgresConnection, typeof(PostgresDbContext))]
    [InlineData(DbSetup.SqlServer, SqlServerConnection, typeof(SqlServerDbContext))]
    public void The_database_context_reads_the_configuration_of_the_built_host(
        string hostProvider, string hostConnection, Type expected)
    {
        // The point of the lazy read: registration is handed the other provider on purpose, and the
        // host is what decides. This is exactly what WebApplicationFactory does — its configuration is
        // layered in when the host is built, long after AddAppDatabase ran.
        using var provider = Provider(
            Configuration(hostProvider, hostConnection),
            Configuration(Other(hostProvider), PostgresConnection));
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.IsType(expected, context);
    }

    [Fact]
    public void An_unknown_provider_is_refused_when_the_job_storage_is_resolved()
    {
        // JobsSetup reads configuration lazily, so the same mistake surfaces on first resolve
        // instead of at registration.
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.ClearProviders());
        services.AddSingleton<IConfiguration>(Configuration(Unknown, PostgresConnection));
        services.AddAppJobs(Configuration(Unknown, PostgresConnection));

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<JobStorage>());

        Assert.Contains(Unknown, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_job_storage_reads_the_configuration_of_the_built_host()
    {
        // The point of the lazy read: what AddAppJobs was handed at registration is not what decides.
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.ClearProviders());
        services.AddSingleton<IConfiguration>(Configuration(Unknown, PostgresConnection));
        services.AddAppJobs(Configuration(DbSetup.Postgres, PostgresConnection));

        using var provider = services.BuildServiceProvider();

        // Registration said Postgres; the host says Oracle, and the host is what Hangfire sees.
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<JobStorage>());
    }

    /// <summary>A container whose host configuration and registration configuration deliberately differ.</summary>
    private static ServiceProvider Provider(IConfiguration host, IConfiguration atRegistration)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.ClearProviders());
        services.AddSingleton(host);
        services.AddAppDatabase(atRegistration);

        return services.BuildServiceProvider();
    }

    private static string Other(string provider) =>
        string.Equals(provider, DbSetup.Postgres, StringComparison.Ordinal) ? DbSetup.SqlServer : DbSetup.Postgres;

    private static IConfiguration Configuration(string provider, string connectionString) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider,
            ["ConnectionStrings:Default"] = connectionString,
        }).Build();
}
