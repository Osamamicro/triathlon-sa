using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Triathlon.Tests.Web;

/// <summary>
/// The staging site is a full copy of the Federation's website with unfinished content on it, so
/// the gate in front of it is the only thing standing between that and a search engine. These run
/// against a second host configured as staging, over the same database container.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class StagingGateTests(WebAppFixture app)
{
    private const string User = "stf";
    private const string Password = "staging-pass-2026";

    [Fact]
    public async Task Public_page_is_challenged_without_credentials()
    {
        using var staging = Staging();
        using var client = staging.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/en");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Without the challenge header a browser shows a blank 401 instead of a credential prompt.
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Basic", challenge.Scheme);
        Assert.Equal("realm=\"staging\"", challenge.Parameter);
    }

    [Fact]
    public async Task Public_page_opens_with_the_configured_credentials()
    {
        using var staging = Staging();
        using var client = staging.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader(User, Password);

        using var response = await client.GetAsync("/en");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"en\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Wrong_password_is_refused()
    {
        using var staging = Staging();
        using var client = staging.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = BasicHeader(User, Password + "!");

        using var response = await client.GetAsync("/en");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_answers_without_credentials()
    {
        using var staging = Staging();
        using var client = staging.CreateClient();

        // The service unit and any load balancer probe this, and neither carries the secret.
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    /// <summary>The same application, configured the way the staging deployment configures it.</summary>
    private WebApplicationFactory<Program> Staging() =>
        app.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Site:Staging"] = "true",
                ["Site:BasicAuth:User"] = User,
                ["Site:BasicAuth:Password"] = Password,
            })));

    private static AuthenticationHeaderValue BasicHeader(string user, string password) =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
}
