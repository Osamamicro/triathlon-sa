using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Services;
using Triathlon.Tests.Web;

namespace Triathlon.Tests.Services;

[Collection(WebAppCollection.Name)]
public sealed class EventsServiceTests(WebAppFixture app)
{
    private static EventsService At(AppDbContext db, string utc) =>
        new(db, new FakeTimeProvider(DateTimeOffset.Parse(utc, null, System.Globalization.DateTimeStyles.AssumeUniversal)));

    [Fact]
    public async Task Seed_loaded_the_prototype_events_and_cities()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = At(db, "2026-09-07T10:00:00Z");

        var cities = await service.CitiesAsync(CancellationToken.None);
        var upcoming = await service.UpcomingAsync(null, null, 0, CancellationToken.None);
        var past = await service.PastAsync(null, null, CancellationToken.None);

        Assert.Equal(8, cities.Count);
        Assert.Equal(10, upcoming.Count);
        Assert.Equal(3, past.Count);
        Assert.Equal("yanbu-openwater-2026", upcoming[0].Slug);
        Assert.Equal("jeddah-kasc-aquathlon-2026", past[0].Slug);   // most recent first
    }

    [Theory]
    // Riyadh is UTC+3. 20:59Z on the 17th is 23:59 Riyadh: race day, still upcoming.
    [InlineData("2026-10-17T20:59:00Z", true)]
    // 21:00Z is 00:00 on the 18th in Riyadh: the race is now past.
    [InlineData("2026-10-17T21:00:00Z", false)]
    public async Task Upcoming_past_boundary_is_midnight_in_Riyadh(string utc, bool stillUpcoming)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var service = At(scope.ServiceProvider.GetRequiredService<AppDbContext>(), utc);

        var upcoming = await service.UpcomingAsync(null, null, 0, CancellationToken.None);

        Assert.Equal(stillUpcoming, upcoming.Any(e => e.Slug == "riyadh-sprint-2026"));
    }

    [Fact]
    public async Task Type_and_city_filters_apply()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var service = At(scope.ServiceProvider.GetRequiredService<AppDbContext>(), "2026-09-07T10:00:00Z");

        var competitions = await service.UpcomingAsync(EventType.Competition, null, 0, CancellationToken.None);
        var riyadh = await service.UpcomingAsync(null, "riyadh", 0, CancellationToken.None);

        Assert.All(competitions, e => Assert.Equal(EventType.Competition, e.Type));
        Assert.Equal(6, competitions.Count);
        Assert.Equal(3, riyadh.Count);
    }

    [Fact]
    public async Task Third_registration_over_a_capacity_of_two_is_waitlisted()
    {
        var slug = "cap-" + Guid.NewGuid().ToString("N")[..8];
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var city = await db.Cities.FirstAsync(c => c.Key == "riyadh");
            db.Events.Add(new Event
            {
                Slug = slug, Type = EventType.Community, IsPublished = true, Season = "2026-27",
                DateStart = new DateOnly(2026, 12, 31), CityId = city.Id,
                TitleEn = "Cap", TitleAr = "سعة", VenueEn = "V", VenueAr = "م", DescriptionEn = "d", DescriptionAr = "و",
                RegistrationMode = RegistrationMode.Internal, RegistrationOpen = true, Capacity = 2,
            });
            await db.SaveChangesAsync();
        }

        var outcomes = new List<RegistrationOutcome>();
        for (var i = 0; i < 3; i++)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var service = At(scope.ServiceProvider.GetRequiredService<AppDbContext>(), "2026-09-07T10:00:00Z");
            outcomes.Add(await service.RegisterAsync(slug,
                new GuestRegistration($"Guest {i}", $"g{i}@x.test", null, "Open", null), CancellationToken.None));
        }

        Assert.Equal([RegistrationOutcome.Confirmed, RegistrationOutcome.Confirmed, RegistrationOutcome.Waitlist], outcomes);
    }

    [Fact]
    public async Task Registration_is_closed_for_soon_and_past_events()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var service = At(scope.ServiceProvider.GetRequiredService<AppDbContext>(), "2026-09-07T10:00:00Z");
        var form = new GuestRegistration("G", "g@x.test", null, "Open", null);

        Assert.Equal(RegistrationOutcome.Closed, await service.RegisterAsync("abha-youth-2026", form, CancellationToken.None));
        Assert.Equal(RegistrationOutcome.Closed, await service.RegisterAsync("jeddah-opener-2026", form, CancellationToken.None));
        Assert.Equal(RegistrationOutcome.NotFound, await service.RegisterAsync("nope", form, CancellationToken.None));
    }

    [Fact]
    public async Task Timeline_defaults_to_the_current_season_and_lists_its_cities()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var service = At(scope.ServiceProvider.GetRequiredService<AppDbContext>(), "2026-09-07T10:00:00Z");

        var timeline = await service.TimelineAsync(null, null, CancellationToken.None);

        Assert.Equal("2026-27", timeline.Season);
        Assert.Equal(["2026-27", "2025-26"], timeline.Seasons);
        Assert.Equal(10, timeline.Events.Count);
        Assert.Equal(8, timeline.Cities.Count);
    }
}
