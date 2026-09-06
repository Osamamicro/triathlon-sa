using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Triathlon.Tests.Web;

/// <summary>
/// The dashboard is the only front door for staff, so these cover the whole cookie round trip:
/// the login page renders, anonymous requests are bounced to it, and the seeded administrator
/// can actually get in through the antiforgery-protected form.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class DashboardAuthTests(WebAppFixture app)
{
    [Fact]
    public async Task Login_page_returns_200()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/dashboard/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_redirects_anonymous_to_login()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/dashboard/login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Seeded_admin_can_sign_in()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginPage = await client.GetStringAsync("/dashboard/login");
        var form = HiddenFields(loginPage);
        Assert.Contains("__RequestVerificationToken", form.Keys);

        form["Input.Email"] = WebAppFixture.AdminEmail;
        form["Input.Password"] = WebAppFixture.AdminPassword;

        using var signIn = await client.PostAsync("/dashboard/login", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Found, signIn.StatusCode);
        Assert.Contains("/dashboard", signIn.Headers.Location!.ToString());
        Assert.DoesNotContain("/dashboard/login", signIn.Headers.Location!.ToString());

        using var dashboard = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains(WebAppFixture.AdminEmail, await dashboard.Content.ReadAsStringAsync());
    }

    /// <summary>Collects every hidden input on a page — the antiforgery token and Blazor's form handler.</summary>
    private static Dictionary<string, string> HiddenFields(string html)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var input in Regex.Matches(html, "<input[^>]*type=\"hidden\"[^>]*>").Cast<Match>())
        {
            var name = Regex.Match(input.Value, "name=\"([^\"]+)\"");
            if (!name.Success)
            {
                continue;
            }

            var value = Regex.Match(input.Value, "value=\"([^\"]*)\"");
            fields[WebUtility.HtmlDecode(name.Groups[1].Value)] =
                WebUtility.HtmlDecode(value.Success ? value.Groups[1].Value : string.Empty);
        }

        return fields;
    }
}
