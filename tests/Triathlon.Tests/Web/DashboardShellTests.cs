using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Triathlon.Tests.Web;

/// <summary>
/// The dashboard shell (MainLayout's nav) built in Task 3.1.G: the interactive-server page still
/// prerenders full HTML on first GET, so these assert straight on the markup — no browser needed.
/// Covers the bilingual IA (English by default, mirrored and translated in Arabic) and that the nav
/// only offers what the signed-in role is actually allowed to open.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class DashboardShellTests(WebAppFixture app)
{
    [Fact]
    public async Task Signed_in_admin_sees_the_events_and_users_nav_links()
    {
        using var client = await DashboardClient.CreateSignedInClientAsync(app);

        using var response = await client.GetAsync("/dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("dashboard/events", html, StringComparison.Ordinal);
        Assert.Contains("dashboard/users", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Arabic_culture_cookie_mirrors_the_shell_and_translates_the_nav()
    {
        using var client = await DashboardClient.CreateSignedInClientAsync(app);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");
        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", html, StringComparison.Ordinal);
        Assert.Contains("الفعاليات", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Editor_does_not_see_the_users_nav_link()
    {
        var (email, password) = await DashboardClient.CreateEditorAsync(app);
        using var client = await DashboardClient.CreateSignedInClientAsync(app, allowAutoRedirect: false, email: email, password: password);

        using var response = await client.GetAsync("/dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("dashboard/users", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Editor_is_redirected_away_from_the_users_page()
    {
        var (email, password) = await DashboardClient.CreateEditorAsync(app);
        using var client = await DashboardClient.CreateSignedInClientAsync(app, allowAutoRedirect: false, email: email, password: password);

        using var response = await client.GetAsync("/dashboard/users");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/dashboard/access-denied", response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_can_open_the_users_page_stub()
    {
        using var client = await DashboardClient.CreateSignedInClientAsync(app);

        using var response = await client.GetAsync("/dashboard/users");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("User management arrives with the CRM in Week 5.", html, StringComparison.Ordinal);
    }
}
