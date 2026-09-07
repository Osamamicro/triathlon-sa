using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Triathlon.Tests.Web;

/// <summary>
/// The two endpoints operations depends on: the health probe the service unit and any load balancer
/// watch, and the Hangfire dashboard — which ships with no authorisation of its own and would
/// otherwise expose job arguments, athlete email addresses included, to anyone who found the URL.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class OperationsEndpointsTests(WebAppFixture app)
{
    [Fact]
    public async Task Health_is_anonymous_and_reports_healthy()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Jobs_dashboard_is_closed_to_anonymous_callers()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/dashboard/jobs");

        // Hangfire answers an unauthorised caller with 401; a redirect to the login page would be
        // just as acceptable. What must never happen is the dashboard rendering.
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Jobs_dashboard_opens_for_the_seeded_super_admin()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = WebAppFixture.HttpsBaseAddress,
        });

        await SignInAsync(client);

        using var response = await client.GetAsync("/dashboard/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hangfire", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>Signs the seeded administrator in through the real antiforgery-protected form.</summary>
    private static async Task SignInAsync(HttpClient client)
    {
        var loginPage = await client.GetStringAsync("/dashboard/login");

        var form = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var input in Regex.Matches(loginPage, "<input[^>]*type=\"hidden\"[^>]*>").Cast<Match>())
        {
            var name = Regex.Match(input.Value, "name=\"([^\"]+)\"");
            if (!name.Success)
            {
                continue;
            }

            var value = Regex.Match(input.Value, "value=\"([^\"]*)\"");
            form[WebUtility.HtmlDecode(name.Groups[1].Value)] =
                WebUtility.HtmlDecode(value.Success ? value.Groups[1].Value : string.Empty);
        }

        form["Input.Email"] = WebAppFixture.AdminEmail;
        form["Input.Password"] = WebAppFixture.AdminPassword;

        using var signIn = await client.PostAsync("/dashboard/login", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Found, signIn.StatusCode);
    }
}
