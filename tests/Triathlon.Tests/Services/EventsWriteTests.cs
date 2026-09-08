using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

[Collection(WebAppCollection.Name)]
public sealed class EventsWriteTests(WebAppFixture app)
{
    private static async Task<Guid> RiyadhAsync(IServiceProvider sp) =>
        (await sp.GetRequiredService<EventsService>().CitiesAsync(CancellationToken.None)).Single(c => c.Key == "riyadh").Id;

    private static EventInput Input(string slug, Guid cityId, bool published = true) => new(
        slug, EventType.Competition, published, "2026-27", new DateOnly(2026, 12, 5), null, new TimeOnly(6, 0), cityId,
        "Write Test Sprint", "سباق الاختبار", "Venue", "الموقع", "Desc", "وصف", "750m", "20km", "5km", ["Elite", "Age Group"],
        RegistrationMode.Internal, true, null, 100, null, "/docs/competition-rules-2026.pdf",
        [new GalleryImageInput(null, "/media/2026/09/a.webp", "Start", "الانطلاق")],
        [new EventResultInput(null, 1, "A. Tester", "أ. المختبر", null, null, "58:41")]);

    [Fact]
    public async Task Publishing_an_event_makes_it_visible_within_one_request_and_logs_the_diff()
    {
        var slug = "evt-" + Guid.NewGuid().ToString("N")[..8];
        using var client = app.CreateClient();
        _ = await client.GetStringAsync("/en/events");
        Assert.True((await client.GetAsync("/en/events")).Headers.Contains("Age"));

        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            id = (await events.CreateAsync(Input(slug, await RiyadhAsync(scope.ServiceProvider), published: false), CancellationToken.None)).Id;
        }

        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync("/en/events/" + slug)).StatusCode);

        await using (var scope = app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<EventsService>().SetPublishedAsync(id, true, CancellationToken.None);

        using var list = await client.GetAsync("/en/events");
        Assert.False(list.Headers.Contains("Age"));
        Assert.Contains("Write Test Sprint", await list.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        var detail = await client.GetStringAsync("/en/events/" + slug);
        Assert.Contains("/media/2026/09/a.webp", detail, StringComparison.Ordinal);
        Assert.Contains("58:41", detail, StringComparison.Ordinal);
        Assert.Contains("href=\"/docs/competition-rules-2026.pdf\"", detail, StringComparison.Ordinal);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.ActivityLogs.Where(l => l.Entity == "Event" && l.EntityId == id.ToString()).OrderBy(l => l.At).ToListAsync();
            Assert.Equal(["create", "publish"], rows.Select(r => r.Action));
            Assert.Contains("\"isPublished\":false", rows[1].Diff, StringComparison.Ordinal);
            Assert.Contains("\"isPublished\":true", rows[1].Diff, StringComparison.Ordinal);
            await scope.ServiceProvider.GetRequiredService<EventsService>().DeleteAsync(id, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Renaming_a_slug_evicts_the_old_page_too_and_bad_input_is_refused()
    {
        var slug = "ren-" + Guid.NewGuid().ToString("N")[..8];
        using var client = app.CreateClient();
        Guid id; Guid city;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            city = await RiyadhAsync(scope.ServiceProvider);
            id = (await scope.ServiceProvider.GetRequiredService<EventsService>().CreateAsync(Input(slug, city), CancellationToken.None)).Id;
        }

        _ = await client.GetStringAsync("/en/events/" + slug);
        Assert.True((await client.GetAsync("/en/events/" + slug)).Headers.Contains("Age"));

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            await Assert.ThrowsAsync<ContentValidationException>(() => events.UpdateAsync(id, Input(slug, city) with { ExternalRegistrationUrl = "javascript:alert(1)", RegistrationMode = RegistrationMode.External }, CancellationToken.None));
            await Assert.ThrowsAsync<ContentValidationException>(() => events.UpdateAsync(id, Input(slug, city) with { ResultsFilePath = "https://evil.example/r.pdf" }, CancellationToken.None));
            await Assert.ThrowsAsync<ContentValidationException>(() => events.UpdateAsync(id, Input("riyadh-sprint-2026", city), CancellationToken.None));
            await events.UpdateAsync(id, Input(slug + "-b", city), CancellationToken.None);
        }

        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync("/en/events/" + slug)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/en/events/" + slug + "-b")).StatusCode);

        await using (var scope = app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<EventsService>().DeleteAsync(id, CancellationToken.None);
    }

    [Fact]
    public async Task Restore_brings_back_the_gallery_and_results_but_never_touched_registrations()
    {
        var slug = "res-" + Guid.NewGuid().ToString("N")[..8];
        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            id = (await events.CreateAsync(Input(slug, await RiyadhAsync(scope.ServiceProvider)), CancellationToken.None)).Id;
            Assert.Equal(RegistrationOutcome.Confirmed, await events.RegisterAsync(slug, new GuestRegistration("R", "r@x.test", null, "Elite", null), CancellationToken.None));
            await events.DeleteAsync(id, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Empty(await db.EventGalleryImages.Where(g => g.EventId == id).ToListAsync());
            Assert.Single(await db.EventRegistrations.Where(r => r.EventId == id).ToListAsync());
            await scope.ServiceProvider.GetRequiredService<EventsService>().RestoreAsync(id, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.NotNull(await db.Events.SingleOrDefaultAsync(e => e.Id == id));
            Assert.Single(await db.EventGalleryImages.Where(g => g.EventId == id).ToListAsync());
            Assert.Single(await db.EventResults.Where(r => r.EventId == id).ToListAsync());
            await scope.ServiceProvider.GetRequiredService<EventsService>().DeleteAsync(id, CancellationToken.None);
        }
    }

    [Fact]
    public async Task A_city_with_events_cannot_be_deleted_but_a_fresh_one_can()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var events = scope.ServiceProvider.GetRequiredService<EventsService>();
        var riyadh = await RiyadhAsync(scope.ServiceProvider);
        await Assert.ThrowsAsync<ContentValidationException>(() => events.DeleteCityAsync(riyadh, CancellationToken.None));

        var city = await events.SaveCityAsync(new CityInput(null, "city-" + Guid.NewGuid().ToString("N")[..6], "New", "جديدة", 100, 100, false, 0, 99), CancellationToken.None);
        await events.DeleteCityAsync(city.Id, CancellationToken.None);
        Assert.DoesNotContain(await events.CitiesAsync(CancellationToken.None), c => c.Id == city.Id);
    }

    [Fact]
    public async Task Delete_stamps_the_event_and_every_child_with_the_exact_same_instant()
    {
        var slug = "del-" + Guid.NewGuid().ToString("N")[..8];
        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            id = (await events.CreateAsync(Input(slug, await RiyadhAsync(scope.ServiceProvider)), CancellationToken.None)).Id;
            await events.DeleteAsync(id, CancellationToken.None);
        }

        await using var scope2 = app.Services.CreateAsyncScope();
        var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = await db.Events.IgnoreQueryFilters().SingleAsync(e => e.Id == id);
        var gallery = await db.EventGalleryImages.IgnoreQueryFilters().Where(g => g.EventId == id).ToListAsync();
        var results = await db.EventResults.IgnoreQueryFilters().Where(r => r.EventId == id).ToListAsync();

        Assert.NotNull(ev.DeletedAt);
        Assert.NotEmpty(gallery);
        Assert.NotEmpty(results);
        Assert.All(gallery, g => Assert.Equal(ev.DeletedAt, g.DeletedAt));
        Assert.All(results, r => Assert.Equal(ev.DeletedAt, r.DeletedAt));
    }

    [Fact]
    public async Task A_refused_update_leaves_the_gallery_and_title_untouched_and_the_scope_clean_for_the_next_save()
    {
        var slugA = "clean-a-" + Guid.NewGuid().ToString("N")[..8];
        var slugB = "clean-b-" + Guid.NewGuid().ToString("N")[..8];
        Guid idA, idB, city;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            city = await RiyadhAsync(scope.ServiceProvider);
            idA = (await events.CreateAsync(Input(slugA, city), CancellationToken.None)).Id;
            idB = (await events.CreateAsync(Input(slugB, city, published: false), CancellationToken.None)).Id;
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            var badInput = Input(slugA, city) with
            {
                TitleAr = "   ",
                Gallery = [new GalleryImageInput(null, "/media/2026/09/b.webp", "New", "جديد")],
            };
            await Assert.ThrowsAsync<ContentValidationException>(() => events.UpdateAsync(idA, badInput, CancellationToken.None));
            // Same scope, a different event: if the refused update above had left a half-applied Event
            // or an orphan Added EventGalleryImage tracked, this save would flush that too.
            await events.SetPublishedAsync(idB, true, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ev = await db.Events.SingleAsync(e => e.Id == idA);
            Assert.Equal("سباق الاختبار", ev.TitleAr);
            Assert.Single(await db.EventGalleryImages.Where(g => g.EventId == idA).ToListAsync());

            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            await events.DeleteAsync(idA, CancellationToken.None);
            await events.DeleteAsync(idB, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Duplicate_ids_in_a_posted_gallery_or_results_list_are_refused()
    {
        var slug = "dup-" + Guid.NewGuid().ToString("N")[..8];
        Guid id, city, galleryId, resultId;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            city = await RiyadhAsync(scope.ServiceProvider);
            id = (await events.CreateAsync(Input(slug, city), CancellationToken.None)).Id;
            var loaded = await events.ForEditAsync(id, CancellationToken.None);
            galleryId = loaded!.Gallery.Single().Id;
            resultId = loaded.Results.Single().Id;
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var events = scope.ServiceProvider.GetRequiredService<EventsService>();
            var dupGallery = Input(slug, city) with
            {
                Gallery = [new GalleryImageInput(galleryId, "/media/2026/09/a.webp", null, null), new GalleryImageInput(galleryId, "/media/2026/09/c.webp", null, null)],
            };
            var galleryEx = await Assert.ThrowsAsync<ContentValidationException>(() => events.UpdateAsync(id, dupGallery, CancellationToken.None));
            Assert.Equal("Validation_DuplicateRow", galleryEx.Key);

            var dupResults = Input(slug, city) with
            {
                Results = [new EventResultInput(resultId, 1, "A", "أ", null, null, "1:00:00"), new EventResultInput(resultId, 2, "B", "ب", null, null, "1:01:00")],
            };
            var resultsEx = await Assert.ThrowsAsync<ContentValidationException>(() => events.UpdateAsync(id, dupResults, CancellationToken.None));
            Assert.Equal("Validation_DuplicateRow", resultsEx.Key);

            await events.DeleteAsync(id, CancellationToken.None);
        }
    }
}
