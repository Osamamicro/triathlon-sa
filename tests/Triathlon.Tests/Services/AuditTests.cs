using Triathlon.Web.Domain.Events;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

public sealed class AuditTests
{
    [Fact]
    public void Snapshot_keeps_scalars_and_drops_stamps_and_navigations()
    {
        var ev = new Event
        {
            Slug = "s", Season = "2026-27", TitleEn = "T", TitleAr = "ت", VenueEn = "V", VenueAr = "م",
            DescriptionEn = "d", DescriptionAr = "و", DateStart = new DateOnly(2026, 10, 17), StartTime = new TimeOnly(6, 0),
            Type = EventType.Competition, CreatedBy = "someone", City = new City { Key = "r", NameEn = "R", NameAr = "ر" },
            Gallery = { new EventGalleryImage { Path = "/media/x.webp" } },
        };

        var snap = Audit.Snapshot(ev);

        Assert.Equal("T", snap["TitleEn"]);
        Assert.Equal(new DateOnly(2026, 10, 17), snap["DateStart"]);
        Assert.Equal(EventType.Competition, snap["Type"]);
        Assert.False(snap.ContainsKey("City"));
        Assert.False(snap.ContainsKey("Gallery"));
        Assert.False(snap.ContainsKey("CategoryList"));
        Assert.False(snap.ContainsKey("CreatedBy"));
        Assert.False(snap.ContainsKey("Id"));
    }
}
