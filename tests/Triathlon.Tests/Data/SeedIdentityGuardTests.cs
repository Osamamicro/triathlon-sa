using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Triathlon.Web.Data.Seed;

namespace Triathlon.Tests.Data;

/// <summary>
/// <c>appsettings.Development.json</c> is in source control, password and all. A server that
/// inherited that file would otherwise come up with an administrator account whose password is
/// public, so the seeder refuses to run outside Development with it.
/// </summary>
public sealed class SeedIdentityGuardTests
{
    [Fact]
    public async Task The_committed_development_password_is_refused_outside_development()
    {
        // No RoleManager and no UserManager are registered: if the guard ever moved below them the
        // failure would be a missing-service exception, not this one.
        var services = Services(Environments.Production, SeedIdentity.DevelopmentPassword);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SeedIdentity.RunAsync(services));

        Assert.Contains("Seed:AdminPassword", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_real_password_gets_past_the_guard()
    {
        var services = Services(Environments.Production, "A-Real-Deployment-Secret-2026!");

        // Past the guard the seeder asks for the role manager, which this bare provider has not got:
        // reaching that failure is what proves the guard let this password through.
        var exception = await Record.ExceptionAsync(() => SeedIdentity.RunAsync(services));

        Assert.Contains("RoleManager", exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Development_still_seeds_with_the_committed_password()
    {
        var services = Services(Environments.Development, SeedIdentity.DevelopmentPassword);

        var exception = await Record.ExceptionAsync(() => SeedIdentity.RunAsync(services));

        Assert.Contains("RoleManager", exception!.Message, StringComparison.Ordinal);
    }

    private static IServiceProvider Services(string environment, string password)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.ClearProviders());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = "admin@triathlon.sa",
                ["Seed:AdminPassword"] = password,
            })
            .Build());
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { EnvironmentName = environment });

        return services.BuildServiceProvider();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Triathlon.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
