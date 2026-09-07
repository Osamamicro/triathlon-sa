using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Events;
using Triathlon.Tests.Web;

namespace Triathlon.Tests.Data;

[Collection(WebAppCollection.Name)]
public sealed class EventsModelTests(WebAppFixture app)
{
    [Fact]
    public async Task Slug_and_city_key_are_unique()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var city = new City { Key = "test-" + Guid.NewGuid().ToString("N")[..8], NameEn = "T", NameAr = "ت" };
        db.Cities.Add(city);
        await db.SaveChangesAsync();

        db.Cities.Add(new City { Key = city.Key, NameEn = "T2", NameAr = "ت٢" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_an_event_soft_deletes_gallery_and_results_but_keeps_registrations()
    {
        Guid eventId;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var city = new City { Key = "cascade-" + Guid.NewGuid().ToString("N")[..8], NameEn = "C", NameAr = "ج" };
            var ev = new Event
            {
                Slug = "cascade-" + Guid.NewGuid().ToString("N")[..8], Type = EventType.Community, IsPublished = true,
                Season = "2026-27", DateStart = new DateOnly(2026, 12, 1), City = city,
                TitleEn = "Cascade", TitleAr = "تسلسل", VenueEn = "V", VenueAr = "م", DescriptionEn = "d", DescriptionAr = "و",
                Gallery = { new EventGalleryImage { Path = "/media/x.webp" } },
                Results = { new EventResult { Position = 1, AthleteEn = "A", AthleteAr = "أ", Time = "1:00:00" } },
                Registrations = { new EventRegistration { FullName = "R", Email = "r@x.test", Category = "Open", Status = RegistrationStatus.Confirmed } },
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();
            eventId = ev.Id;
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var service = scope.ServiceProvider.GetRequiredService<Triathlon.Web.Services.EventsService>();
            await service.DeleteAsync(eventId, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Null(await db.Events.FirstOrDefaultAsync(e => e.Id == eventId));
            Assert.Empty(await db.EventGalleryImages.Where(g => g.EventId == eventId).ToListAsync());
            Assert.Empty(await db.EventResults.Where(r => r.EventId == eventId).ToListAsync());
            Assert.Single(await db.EventRegistrations.Where(r => r.EventId == eventId).ToListAsync());
            Assert.NotNull((await db.EventGalleryImages.IgnoreQueryFilters().SingleAsync(g => g.EventId == eventId)).DeletedAt);
        }
    }
}
