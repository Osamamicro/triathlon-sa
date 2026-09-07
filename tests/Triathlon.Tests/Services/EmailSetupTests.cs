using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// Which sender gets registered is decided once, from configuration. Getting it backwards would
/// either silently swallow production mail or make a developer machine unable to start, so the
/// choice is worth pinning down.
/// </summary>
public sealed class EmailSetupTests
{
    [Fact]
    public void No_host_configured_falls_back_to_the_logging_sender()
    {
        var sender = Resolve([]);

        Assert.IsType<LoggingEmailSender>(sender);
    }

    [Fact]
    public void A_configured_host_selects_the_smtp_sender()
    {
        var sender = Resolve(new Dictionary<string, string?>
        {
            ["Email:Host"] = "smtp.triathlon.sa",
            ["Email:FromAddress"] = "no-reply@triathlon.sa",
        });

        Assert.IsType<SmtpEmailSender>(sender);
    }

    private static IEmailSender Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging(logging => logging.ClearProviders());
        services.AddAppEmail(configuration);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IEmailSender>();
    }
}
