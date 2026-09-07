using System.Net;
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
    public async Task Login_email_field_carries_an_explicit_type_so_the_stylesheet_matches_it()
    {
        // dashboard.css styles the email field with the attribute selector
        // `input[type="email"]` — InputText emits no `type` attribute at all unless one is passed
        // explicitly, so without it the field renders as an unstyled, browser-default text box.
        using var client = app.CreateClient();

        var html = await client.GetStringAsync("/dashboard/login");

        Assert.Contains("type=\"email\"", html, StringComparison.Ordinal);
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
        // The identity cookie is Secure-only (see Program.cs), so this client must talk "https"
        // for the cookie set on sign-in to actually come back on the follow-up GET /dashboard.
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = WebAppFixture.HttpsBaseAddress,
        });

        var loginPage = await client.GetStringAsync("/dashboard/login");
        var form = DashboardClient.HiddenFields(loginPage);
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

    [Fact]
    public async Task Login_never_redirects_off_host_even_with_a_protocol_relative_returnUrl()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = WebAppFixture.HttpsBaseAddress,
        });

        var loginPage = await client.GetStringAsync("/dashboard/login?returnUrl=%2F%2Fevil.com");
        var form = DashboardClient.HiddenFields(loginPage);
        Assert.Contains("__RequestVerificationToken", form.Keys);

        form["Input.Email"] = WebAppFixture.AdminEmail;
        form["Input.Password"] = WebAppFixture.AdminPassword;

        using var signIn = await client.PostAsync("/dashboard/login?returnUrl=%2F%2Fevil.com", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Found, signIn.StatusCode);
        Assert.Contains("/dashboard", signIn.Headers.Location!.ToString());
        Assert.DoesNotContain("evil.com", signIn.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Logout_with_a_tampered_returnUrl_lands_on_the_login_page()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = WebAppFixture.HttpsBaseAddress,
        });

        await DashboardClient.SignInAsync(client);

        // The sign-out form is rendered by the dashboard layout, so its antiforgery token comes from
        // the signed-in page — the same token the browser would post.
        var dashboard = await client.GetStringAsync("/dashboard");
        var form = DashboardClient.HiddenFields(dashboard);
        Assert.Contains("__RequestVerificationToken", form.Keys);

        // What a hand-edited form would post. LocalRedirect throws on it, so an unguarded endpoint
        // answers 500 and leaves the visitor signed out on an error page.
        form["returnUrl"] = "//evil.com";

        using var logout = await client.PostAsync("/dashboard/logout", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Found, logout.StatusCode);
        Assert.Equal("/dashboard/login", logout.Headers.Location!.ToString());

        // And it really was a sign-out: the dashboard bounces us back to the login page again.
        using var afterLogout = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Found, afterLogout.StatusCode);
        Assert.Contains("/dashboard/login", afterLogout.Headers.Location!.ToString(), StringComparison.Ordinal);
    }
}
