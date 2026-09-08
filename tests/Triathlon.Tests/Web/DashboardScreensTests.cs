using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// The Task 3.2.A page chrome (<c>EntityTable</c>, <c>EditShell</c>) and the events/cities screens
/// built on it: the interactive-server pages still prerender full HTML on first GET, so these
/// assert straight on the markup, the same way <see cref="DashboardShellTests"/> does for the shell.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class DashboardScreensTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/dashboard/events", "Riyadh Sprint Triathlon")]
    [InlineData("/dashboard/cities", "riyadh")]
    [InlineData("/dashboard/documents", "Annual Report 2025")]
    [InlineData("/dashboard/rules", "competition-rules-2026")]
    [InlineData("/dashboard/guides", "beginner-12-weeks")]
    [InlineData("/dashboard/media", "Upload")]
    [InlineData("/dashboard/pages", "join")]
    [InlineData("/dashboard/news", "kasc-sunset-aquathlon-recap")]
    [InlineData("/dashboard/governance", "Board of Directors")]
    [InlineData("/dashboard/navigation", "events/timeline")]
    [InlineData("/dashboard/statistics", "athletes")]
    public async Task List_screens_prerender_seeded_rows(string path, string expected)
    {
        using var client = await DashboardClient.CreateSignedInClientAsync(app, allowAutoRedirect: true);
        var html = await client.GetStringAsync(path);
        Assert.Contains(expected, html, StringComparison.Ordinal);
        Assert.Contains("<!--Blazor:", html, StringComparison.Ordinal); // interactive-server prerender marker
    }

    [Fact]
    public async Task Event_edit_screen_prerenders_the_form_in_both_directions()
    {
        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var rows = await scope.ServiceProvider.GetRequiredService<EventsService>().AllForEditAsync(false, CancellationToken.None);
            id = rows.Single(r => r.Event.Slug == "riyadh-sprint-2026").Event.Id;
        }

        using var client = await DashboardClient.CreateSignedInClientAsync(app, allowAutoRedirect: true);
        var en = await client.GetStringAsync($"/dashboard/events/{id}");
        Assert.Contains("value=\"Riyadh Sprint Triathlon\"", en, StringComparison.Ordinal);
        Assert.Contains("Publish", en, StringComparison.Ordinal);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/dashboard/events/{id}");
        request.Headers.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");
        using var response = await client.SendAsync(request);
        var ar = await response.Content.ReadAsStringAsync();
        Assert.Contains("dir=\"rtl\"", ar, StringComparison.Ordinal);
        Assert.Contains("نشر", ar, StringComparison.Ordinal); // "Publish" in Arabic (resx value)
    }

    [Fact]
    public async Task Editor_cannot_open_users_but_can_open_events()
    {
        var (email, password) = await DashboardClient.CreateEditorAsync(app);
        using var client = await DashboardClient.CreateSignedInClientAsync(app, allowAutoRedirect: false, email: email, password: password);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/dashboard/events")).StatusCode);
        using var users = await client.GetAsync("/dashboard/users");
        Assert.Equal(HttpStatusCode.Found, users.StatusCode);
        Assert.Contains("/dashboard/access-denied", users.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Settings_screen_is_admin_only()
    {
        using var adminClient = await DashboardClient.CreateSignedInClientAsync(app, allowAutoRedirect: true);
        var html = await adminClient.GetStringAsync("/dashboard/settings");
        Assert.Contains("contact.email", html, StringComparison.Ordinal);

        var (email, password) = await DashboardClient.CreateEditorAsync(app);
        using var editorClient = await DashboardClient.CreateSignedInClientAsync(app, allowAutoRedirect: false, email: email, password: password);
        using var settings = await editorClient.GetAsync("/dashboard/settings");
        Assert.Equal(HttpStatusCode.Found, settings.StatusCode);
        Assert.Contains("/dashboard/access-denied", settings.Headers.Location!.ToString(), StringComparison.Ordinal);
    }
}
