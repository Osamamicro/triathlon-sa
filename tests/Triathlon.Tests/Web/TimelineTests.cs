using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Tests.Web;

/// <summary>
/// The season timeline: the page itself (items and map markers rendered by the server, script only
/// toggling classes on top), the JSON the same data is published as, and the ICS calendar feed.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class TimelineTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/en/events/timeline", "The Season, Mapped", "RIYADH")]
    [InlineData("/ar/events/timeline", "الموسم على الخريطة", "الرياض")]
    public async Task Timeline_page_renders_items_and_markers_server_side(string path, string heading, string marker)
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync(path);

        Assert.Contains(heading, html, StringComparison.Ordinal);
        Assert.Contains("class=\"tl-item t-competition\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"marker m-competition\"", html, StringComparison.Ordinal);
        Assert.Contains(marker, html, StringComparison.Ordinal);
        Assert.Contains("js/timeline.js", html, StringComparison.Ordinal);
        // The map answers the scroll position, and says so: timeline.js writes the city into a
        // live region whose sentence the server supplies in the page's own culture.
        Assert.Contains("id=\"timelineLive\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", html, StringComparison.Ordinal);
        // 26 user units of transparent circle: the pointer target SC 2.5.8 asks for.
        Assert.Contains("class=\"hit\"", html, StringComparison.Ordinal);
        Assert.False(Markup.HasInlineScript(html));
    }

    [Fact]
    public async Task Timeline_page_lists_a_month_heading_and_a_marker_per_city_of_the_season()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/en/events/timeline");

        // The filter has nothing to hide unless the type each item and marker belongs to is on it.
        Assert.Contains("data-month=\"2026-10\"", html, StringComparison.Ordinal);
        Assert.Contains("data-city=\"riyadh\" data-type=\"competition community\"", html, StringComparison.Ordinal);
        Assert.Contains("data-city=\"neom\" data-type=\"competition\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/en/events/riyadh-sprint-2026\"", html, StringComparison.Ordinal);

        // Single-culture markup, still (Week 1 ruling), and the card is a real link.
        Assert.DoesNotContain("class=\"en\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"link\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Timeline_json_lists_the_season_in_the_requested_culture()
    {
        var expected = await CountAsync(e => e.Season == "2026-27");

        using var client = app.CreateClient();
        var json = await client.GetFromJsonAsync<JsonElement>("/api/timeline?culture=ar");

        Assert.Equal("2026-27", json.GetProperty("season").GetString());
        Assert.Equal(expected, json.GetProperty("events").GetArrayLength());
        Assert.Contains(json.GetProperty("cities").EnumerateArray(), c => c.GetProperty("name").GetString() == "الرياض");
        Assert.Contains(json.GetProperty("events").EnumerateArray(),
            e => e.GetProperty("url").GetString() == "/ar/events/riyadh-sprint-2026");
    }

    [Fact]
    public async Task Ics_feed_has_one_vevent_per_published_event_and_honours_type()
    {
        var expectedAll = await CountAsync(_ => true);
        var expectedCompetitions = await CountAsync(e => e.Type == EventType.Competition);

        using var client = app.CreateClient();
        using var all = await client.GetAsync("/api/calendar.ics");
        var text = await all.Content.ReadAsStringAsync();
        var competitions = await client.GetStringAsync("/api/calendar.ics?type=competition");

        Assert.Equal("text/calendar", all.Content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", all.Content.Headers.ContentType!.CharSet);
        Assert.Equal(expectedAll, text.Split("BEGIN:VEVENT").Length - 1);
        Assert.Equal(expectedCompetitions, competitions.Split("BEGIN:VEVENT").Length - 1);
        Assert.Contains("UID:riyadh-sprint-2026@triathlon.sa", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// How many published events the feed should carry, straight from the database. Other classes
    /// in this collection create their own events with generated slugs and clean them up again, so
    /// a hard-coded seed count would make this class depend on the order the suite happens to run
    /// in; those slugs are prefixed, and excluded here.
    /// </summary>
    private async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Event, bool>> narrow)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Events.AsNoTracking()
            .Where(e => e.IsPublished && !e.Slug.StartsWith("cap-") && !e.Slug.StartsWith("cascade-"))
            .Where(narrow)
            .CountAsync();
    }
}
