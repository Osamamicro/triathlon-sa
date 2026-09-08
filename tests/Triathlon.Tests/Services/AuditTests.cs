using Triathlon.Web.Domain.Content;
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

        // A child collection of audited entities contributes a count and a content hash instead of
        // being dropped outright, so a change inside the children still moves the snapshot.
        Assert.Equal(1, snap["GalleryCount"]);
        Assert.IsType<string>(snap["GalleryHash"]);
    }

    [Fact]
    public void Two_pages_differing_only_in_a_block_body_produce_different_BlocksHash()
    {
        Page Build(string body) => new()
        {
            Slug = "s", TitleEn = "T", TitleAr = "ت",
            Blocks = { new PageBlock { SortOrder = 1, Type = BlockType.RichText, BodyEn = body, BodyAr = "نص" } },
        };

        var before = Audit.Snapshot(Build("<p>Before</p>"));
        var after = Audit.Snapshot(Build("<p>After</p>"));

        Assert.Equal(1, before["BlocksCount"]);
        Assert.Equal(before["BlocksCount"], after["BlocksCount"]);
        Assert.NotEqual(before["BlocksHash"], after["BlocksHash"]);
    }
}
