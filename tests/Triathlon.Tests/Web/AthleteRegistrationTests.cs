using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Crm;

namespace Triathlon.Tests.Web;

/// <summary>
/// Athlete registration end to end: the form the join page sends visitors to, and the endpoint
/// behind it, which has to survive antiforgery, the rate limiter, validation and a mail send that
/// must never cost an applicant their application.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class AthleteRegistrationTests(WebAppFixture app)
{
    [Fact]
    public async Task Form_renders_cities_clubs_and_upcoming_events_and_prefills_the_event()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/ar/register?event=riyadh-sprint-2026");
        var eventId = await EventIdAsync("riyadh-sprint-2026");

        Assert.Contains("الرياض", html, StringComparison.Ordinal);                    // a seeded city
        Assert.Contains("نادي الرياض للترايثلون", html, StringComparison.Ordinal);      // a seeded club
        // The select carries event ids, so the prefill is only provable against the real row.
        Assert.Contains($"<option value=\"{eventId}\" selected", html, StringComparison.Ordinal);
        Assert.DoesNotContain("cf-turnstile", html, StringComparison.Ordinal);        // no keys configured in tests
    }

    [Fact]
    public async Task Valid_application_creates_a_pending_athlete_and_sends_an_email()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());
        var email = $"athlete-{Guid.NewGuid():N}@example.test";

        using var response = await Forms.PostFormAsync(client, "/en/register", "/api/register", new()
        {
            ["culture"] = "en", ["fullName"] = "Test Athlete", ["email"] = email, ["dateOfBirth"] = "1995-04-12",
            ["city"] = "riyadh", ["category"] = "Age Group", ["club"] = "", ["event"] = "", ["declaration"] = "on",
        });

        Assert.EndsWith("/en/register/received?name=Test%20Athlete",
            response.RequestMessage!.RequestUri!.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("Test Athlete", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        await using var scope = app.Services.CreateAsyncScope();
        var athlete = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Athletes.SingleAsync(a => a.Email == email);

        Assert.Equal(AthleteStatus.Pending, athlete.Status);
        Assert.Equal(new DateOnly(1995, 4, 12), athlete.DateOfBirth);
        Assert.Equal("riyadh", athlete.CityKey);

        var sent = app.Emails.Single(m => m.To == email);
        Assert.Contains("Saudi Triathlon", sent.Subject, StringComparison.Ordinal);
        Assert.Contains("Test Athlete", sent.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Arabic_application_is_confirmed_in_Arabic()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());
        var email = $"athlete-{Guid.NewGuid():N}@example.test";

        using var response = await Forms.PostFormAsync(client, "/ar/register", "/api/register", new()
        {
            ["culture"] = "ar", ["fullName"] = "سالم الحربي", ["email"] = email, ["dateOfBirth"] = "1998-02-03",
            ["city"] = "jeddah", ["category"] = "Elite", ["club"] = "", ["event"] = "", ["declaration"] = "on",
        });

        Assert.StartsWith("/ar/register/received", response.RequestMessage!.RequestUri!.PathAndQuery, StringComparison.Ordinal);

        var sent = app.Emails.Single(m => m.To == email);
        Assert.Contains("الاتحاد السعودي للترايثلون", sent.Subject, StringComparison.Ordinal);
        // The applicant's own name rather than a wall of numeric entities: the mail is the one place
        // a person reads it, so the body encoder has to leave Arabic alone.
        Assert.Contains("سالم الحربي", sent.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_declaration_bounces_back_to_the_form()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await Forms.PostFormAsync(client, "/en/register", "/api/register", new()
        {
            ["culture"] = "en", ["fullName"] = "X", ["email"] = "x@x.test", ["dateOfBirth"] = "2000-01-01",
            ["city"] = "riyadh", ["category"] = "Elite",
        });

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/en/register?invalid=1", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Rejected_application_keeps_what_the_visitor_typed_and_flags_the_failing_fields()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await Forms.PostFormAsync(client, "/en/register", "/api/register", new()
        {
            // Born in 1890, and a category no form ever offered.
            ["culture"] = "en", ["fullName"] = "Round Trip Athlete", ["email"] = "round-trip@example.test",
            ["dateOfBirth"] = "1890-01-01", ["city"] = "riyadh", ["category"] = "Not A Real Category",
            ["club"] = "", ["event"] = "", ["declaration"] = "on",
        });

        Assert.EndsWith("/en/register?invalid=1", response.RequestMessage!.RequestUri!.PathAndQuery, StringComparison.Ordinal);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("value=\"Round Trip Athlete\"", html, StringComparison.Ordinal);
        Assert.Contains("field invalid", FieldWrapper(html, "id=\"dateOfBirth\""), StringComparison.Ordinal);
        Assert.Contains("field invalid", FieldWrapper(html, "id=\"category\""), StringComparison.Ordinal);
        Assert.DoesNotContain("invalid", FieldWrapper(html, "id=\"fullName\""), StringComparison.Ordinal);
        Assert.DoesNotContain(app.Emails, m => m.To == "round-trip@example.test");
    }

    [Fact]
    public async Task A_child_too_young_to_hold_a_licence_is_refused()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());
        var born = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3);

        using var response = await Forms.PostFormAsync(client, "/en/register", "/api/register", new()
        {
            ["culture"] = "en", ["fullName"] = "Too Young", ["email"] = "too-young@example.test",
            ["dateOfBirth"] = born.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["city"] = "riyadh", ["category"] = "Youth", ["club"] = "", ["event"] = "", ["declaration"] = "on",
        });

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/en/register?invalid=1", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task A_club_that_is_not_on_the_list_is_refused()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await Forms.PostFormAsync(client, "/en/register", "/api/register", new()
        {
            ["culture"] = "en", ["fullName"] = "Crafted Post", ["email"] = "crafted@example.test",
            ["dateOfBirth"] = "1990-06-01", ["city"] = "riyadh", ["category"] = "Elite",
            ["club"] = Guid.NewGuid().ToString(), ["event"] = "", ["declaration"] = "on",
        });

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/en/register?invalid=1", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_refused()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await client.PostAsync("/api/register", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["culture"] = "en", ["fullName"] = "No Token", ["email"] = "no-token@example.test",
                ["dateOfBirth"] = "1990-06-01", ["city"] = "riyadh", ["category"] = "Elite", ["declaration"] = "on",
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Configured_keys_put_the_widget_on_the_form_and_Cloudflare_in_the_policy()
    {
        // The whole enabled branch — widget, script tag, CSP — is dead code on a machine without a
        // key pair, so it is exercised on a host that has one.
        using var challenged = app.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Turnstile:SiteKey"] = "1x00000000000000000000AA",
                ["Turnstile:SecretKey"] = "1x0000000000000000000000000000000AA",
            })));
        using var client = challenged.CreateClient();

        using var response = await client.GetAsync("/en/register");
        var html = await response.Content.ReadAsStringAsync();
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();

        Assert.Contains("class=\"cf-turnstile\" data-sitekey=\"1x00000000000000000000AA\"", html, StringComparison.Ordinal);
        Assert.Contains("https://challenges.cloudflare.com/turnstile/v0/api.js", html, StringComparison.Ordinal);
        Assert.Contains("script-src 'self' https://challenges.cloudflare.com", csp, StringComparison.Ordinal);
        Assert.Contains("frame-src https://challenges.cloudflare.com", csp, StringComparison.Ordinal);
    }

    private async Task<Guid> EventIdAsync(string slug)
    {
        await using var scope = app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Events.Where(e => e.Slug == slug).Select(e => e.Id).SingleAsync();
    }

    /// <summary>The nearest enclosing field wrapper before the element identified by <paramref name="marker"/>.</summary>
    private static string FieldWrapper(string html, string marker)
    {
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"'{marker}' not found in the response body.");

        var divIndex = html.LastIndexOf("<div class=\"field", markerIndex, StringComparison.Ordinal);
        Assert.True(divIndex >= 0, $"No enclosing field wrapper found before '{marker}'.");

        return html[divIndex..html.IndexOf('>', divIndex)];
    }
}
