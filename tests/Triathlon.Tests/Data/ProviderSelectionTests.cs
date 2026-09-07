using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Triathlon.Web.Data;
using Triathlon.Web.Jobs;

namespace Triathlon.Tests.Data;

/// <summary>
/// One configuration key picks the database provider for both EF and Hangfire. A typo in it must
/// stop the application with a sentence that says what to do, not start a site that silently cannot
/// reach its data.
/// </summary>
public sealed class ProviderSelectionTests
{
    private const string Unknown = "Oracle";

    [Fact]
    public void An_unknown_provider_is_refused_when_the_database_is_registered()
    {
        // DbSetup chooses the provider while registering, so this throws there and never builds.
        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddAppDatabase(Configuration()));

        Assert.Contains(Unknown, exception.Message, StringComparison.Ordinal);
        Assert.Contains(DbSetup.Postgres, exception.Message, StringComparison.Ordinal);
        Assert.Contains(DbSetup.SqlServer, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_provider_is_refused_when_the_job_storage_is_resolved()
    {
        // JobsSetup reads configuration lazily, so the same mistake surfaces on first resolve
        // instead of at registration.
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.ClearProviders());
        services.AddSingleton<IConfiguration>(Configuration());
        services.AddAppJobs(Configuration());

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
        services.AddSingleton<IConfiguration>(Configuration());
        services.AddAppJobs(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = DbSetup.Postgres,
            ["ConnectionStrings:Default"] = "Host=nowhere;Database=triathlon;Username=u;Password=p",
        }).Build());

        using var provider = services.BuildServiceProvider();

        // Registration said Postgres; the host says Oracle, and the host is what Hangfire sees.
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<JobStorage>());
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = Unknown,
            ["ConnectionStrings:Default"] = "Host=nowhere;Database=triathlon;Username=u;Password=p",
        }).Build();
}
