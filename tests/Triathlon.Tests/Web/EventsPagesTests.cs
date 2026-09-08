using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// The events section end to end: both calendars in both languages, the filters, an event page,
/// and the guest entry that has to survive antiforgery, validation and the rate limiter.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class EventsPagesTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/en/events", "Riyadh Sprint Triathlon")]
    [InlineData("/ar/events", "ترايثلون الرياض للمسافة القصيرة")]
    public async Task Events_list_renders_seeded_events_in_the_culture(string path, string title)
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync(path);

        Assert.Contains(title, html, StringComparison.Ordinal);
        Assert.Contains("href=\"" + path[..3] + "/events/riyadh-sprint-2026\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"en\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Type_filter_hides_the_other_calendar()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en/events?type=competition");

        Assert.Contains("Riyadh Sprint Triathlon", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Yanbu Open Water Festival", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task City_filter_narrows_both_calendars_and_shows_the_empty_note()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en/events?type=community&city=riyadh");

        Assert.Contains("Riyadh Community Aquathlon", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Yanbu Open Water Festival", html, StringComparison.Ordinal);

        // No community event in Riyadh has been raced yet, so the archive shows its empty note.
        Assert.Contains("No past events match this filter.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Detail_page_shows_key_information_and_results_for_a_completed_event()
    {
        using var client = app.CreateClient();
        var upcoming = await client.GetStringAsync("/en/events/riyadh-sprint-2026");
        var done = await client.GetStringAsync("/ar/events/jeddah-opener-2026");

        Assert.Contains("King Salman Park circuit", upcoming, StringComparison.Ordinal);
        Assert.Contains("href=\"/en/events/riyadh-sprint-2026/register\"", upcoming, StringComparison.Ordinal);
        Assert.Contains("س. الحربي", done, StringComparison.Ordinal);
        Assert.Contains("58:41", done, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_slug_is_the_branded_404()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/en/events/no-such-race");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Culture_less_event_url_is_not_served()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/events/riyadh-sprint-2026");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Entry_form_for_an_event_that_is_not_taking_entries_redirects_to_the_event()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/en/events/abha-youth-2026/register");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/en/events/abha-youth-2026", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Guest_entry_confirms_and_lands_on_the_confirmation_page()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await Forms.PostFormAsync(client, "/en/events/riyadh-sprint-2026/register",
            "/api/events/riyadh-sprint-2026/register", new()
            {
                // The optional fields are posted empty, exactly as a browser posts them when the
                // visitor leaves them alone — [Phone] rejects "" as readily as a malformed number.
                ["culture"] = "en", ["fullName"] = "Test Guest", ["email"] = "guest@example.test",
                ["category"] = "Age Group", ["phone"] = "", ["club"] = "", ["declaration"] = "on",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);     // followed the redirect
        var pathAndQuery = response.RequestMessage!.RequestUri!.PathAndQuery;
        Assert.StartsWith("/en/events/riyadh-sprint-2026/registered?outcome=confirmed", pathAndQuery, StringComparison.Ordinal);
        // The visitor's name travels through TempData, not the redirect's query string.
        Assert.DoesNotContain("name=", pathAndQuery, StringComparison.Ordinal);
        Assert.Contains("Test Guest", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Incomplete_entry_comes_back_to_the_form()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await Forms.PostFormAsync(client, "/en/events/riyadh-sprint-2026/register",
            "/api/events/riyadh-sprint-2026/register", new()
            {
                ["culture"] = "en", ["fullName"] = "Test Guest", ["email"] = "not-an-email",
                ["category"] = "Age Group", ["club"] = "", ["declaration"] = "on",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.EndsWith("/en/events/riyadh-sprint-2026/register?invalid=1",
            response.RequestMessage!.RequestUri!.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("Please check the highlighted fields and try again.",
            await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejected_entry_keeps_what_the_visitor_typed_and_flags_the_failing_fields()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await Forms.PostFormAsync(client, "/en/events/riyadh-sprint-2026/register",
            "/api/events/riyadh-sprint-2026/register", new()
            {
                ["culture"] = "en", ["fullName"] = "Round Trip Guest", ["email"] = "not-an-email",
                ["category"] = "Age Group", ["club"] = "",
                // "declaration" is omitted: an unchecked box is simply absent from a browser post.
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("value=\"Round Trip Guest\"", html, StringComparison.Ordinal);
        Assert.Contains("field invalid", FieldWrapper(html, "id=\"email\""), StringComparison.Ordinal);
        Assert.Contains("field invalid", FieldWrapper(html, "name=\"declaration\""), StringComparison.Ordinal);
        Assert.DoesNotContain("invalid", FieldWrapper(html, "id=\"fullName\""), StringComparison.Ordinal);
    }

    /// <summary>The nearest enclosing `&lt;div class="field...\"&gt;` before the element identified by <paramref name="marker"/>.</summary>
    private static string FieldWrapper(string html, string marker)
    {
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"'{marker}' not found in the response body.");

        var divIndex = html.LastIndexOf("<div class=\"field", markerIndex, StringComparison.Ordinal);
        Assert.True(divIndex >= 0, $"No enclosing field wrapper found before '{marker}'.");

        return html[divIndex..html.IndexOf('>', divIndex)];
    }

    [Fact]
    public async Task Arabic_guest_entry_localises_the_event_categories()
    {
        // riyadh-sprint-2026 is seeded with "Elite,Age Group,Junior" (SeedEvents.cs) — raw English
        // keys the endpoint validates against. The visible option text must be translated even
        // though the posted value stays the English key; the athlete registration form
        // (/ar/register) already gets this right via AthleteCategories.Arabic.
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/ar/events/riyadh-sprint-2026/register");

        Assert.Contains(">النخبة<", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Elite<", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Category_not_offered_by_the_event_is_rejected()
    {
        // A crafted POST (or a stale option from a form the event changed under) must not be able
        // to write an arbitrary category string onto a registration.
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await Forms.PostFormAsync(client, "/en/events/riyadh-sprint-2026/register",
            "/api/events/riyadh-sprint-2026/register", new()
            {
                ["culture"] = "en", ["fullName"] = "Category Guest", ["email"] = "guest@example.test",
                ["category"] = "Not A Real Category", ["club"] = "", ["declaration"] = "on",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.EndsWith("/en/events/riyadh-sprint-2026/register?invalid=1",
            response.RequestMessage!.RequestUri!.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains("field invalid",
            FieldWrapper(await response.Content.ReadAsStringAsync(), "id=\"category\""), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_refused()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        using var response = await client.PostAsync("/api/events/riyadh-sprint-2026/register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["culture"] = "en", ["fullName"] = "x", ["email"] = "x@x.test", ["category"] = "Open",
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Eleventh_entry_from_one_address_in_a_minute_is_rate_limited()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());

        HttpResponseMessage? last = null;
        for (var i = 1; i <= 11; i++)
        {
            last?.Dispose();
            last = await Forms.PostFormAsync(client, "/en/events/riyadh-sprint-2026/register",
                "/api/events/riyadh-sprint-2026/register", new()
                {
                    ["culture"] = "en", ["fullName"] = $"Guest {i}", ["email"] = $"g{i}@example.test",
                    ["category"] = "Open", ["declaration"] = "on",
                });
            if (i <= 10) Assert.Equal(HttpStatusCode.Found, last.StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        last.Dispose();
    }

    [Fact]
    public async Task Detail_page_is_cached_and_dropped_by_its_own_event_tag()
    {
        using var client = app.CreateClient();

        using var first = await client.GetAsync("/en/events/riyadh-sprint-2026");
        using var second = await client.GetAsync("/en/events/riyadh-sprint-2026");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.True(second.Headers.Contains("Age"), "The second GET of the event page was not served from the output cache.");

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();
            await store.EvictAsync(CancellationToken.None, CacheTags.Event("riyadh-sprint-2026"));
        }

        using var afterEviction = await client.GetAsync("/en/events/riyadh-sprint-2026");

        Assert.Equal(HttpStatusCode.OK, afterEviction.StatusCode);
        Assert.False(afterEviction.Headers.Contains("Age"), "The event page was still cached after its own tag was evicted.");
    }

    [Fact]
    public async Task Home_shows_the_next_three_events_from_the_database()
    {
        // The seeded dates are fixed and the fixture runs on the real clock, so the three cards the
        // home page carries are whatever the service says they are today, not a hard-coded list.
        string[] next;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            next = (await events.UpcomingAsync(null, null, 4, CancellationToken.None))
                .Select(e => e.TitleEn).ToArray();
        }

        Assert.Equal(4, next.Length);

        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en");

        Assert.Contains(next[0], html, StringComparison.Ordinal);
        Assert.Contains(next[1], html, StringComparison.Ordinal);
        Assert.Contains(next[2], html, StringComparison.Ordinal);
        Assert.DoesNotContain(next[3], html, StringComparison.Ordinal);
        Assert.DoesNotContain("data.js", html, StringComparison.Ordinal);
    }
}
