using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Areas.Public;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Services;

/// <summary>The dashboard's write side for events and cities: create, update, publish, delete and restore, gallery and results included.</summary>
public sealed partial class EventsService
{
    public async Task<IReadOnlyList<Event>> AllForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.Events.IgnoreQueryFilters().Where(e => e.DeletedAt != null) : db.Events)
            .AsNoTracking().Include(e => e.City).OrderByDescending(e => e.DateStart).ToListAsync(ct);

    public Task<Event?> ForEditAsync(Guid id, CancellationToken ct) =>
        db.Events.Include(e => e.City).Include(e => e.Gallery.OrderBy(g => g.SortOrder)).Include(e => e.Results.OrderBy(r => r.Position))
            .SingleOrDefaultAsync(e => e.Id == id, ct);

    public Task<int> RegistrationCountAsync(Guid eventId, CancellationToken ct) =>
        db.EventRegistrations.CountAsync(r => r.EventId == eventId && r.Status != RegistrationStatus.Cancelled, ct);

    public async Task<Event> CreateAsync(EventInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ev = new Event { Slug = input.Slug, Season = "", TitleEn = "", TitleAr = "", VenueEn = "", VenueAr = "", DescriptionEn = "", DescriptionAr = "" };
        await ApplyAsync(ev, input, ct);
        db.Events.Add(ev);
        await commit.ApplyAsync("Event", ev.Id, "create", null, Audit.Snapshot(ev), EventTags(ev.Slug), ct);
        return ev;
    }

    public async Task UpdateAsync(Guid id, EventInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ev = await ForEditAsync(id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound");
        var before = Audit.Snapshot(ev);
        var oldSlug = ev.Slug;
        await ApplyAsync(ev, input, ct);
        var tags = EventTags(ev.Slug).Append(CacheTags.Event(oldSlug)).Distinct().ToArray();
        await commit.ApplyAsync("Event", ev.Id, "update", before, Audit.Snapshot(ev), tags, ct);
    }

    public async Task SetPublishedAsync(Guid id, bool published, CancellationToken ct)
    {
        var ev = await db.Events.SingleOrDefaultAsync(e => e.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound");
        var before = Audit.Snapshot(ev);
        ev.IsPublished = published;
        await commit.ApplyAsync("Event", ev.Id, published ? "publish" : "unpublish", before, Audit.Snapshot(ev), EventTags(ev.Slug), ct);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct)
    {
        var ev = await db.Events.IgnoreQueryFilters().Include(e => e.Gallery).Include(e => e.Results)
            .SingleOrDefaultAsync(e => e.Id == id && e.DeletedAt != null, ct);
        if (ev is null) return;
        Restore.Aggregate(ev, ev.Gallery.Cast<BaseEntity>().Concat(ev.Results));
        await commit.ApplyAsync("Event", ev.Id, "restore", null, Audit.Snapshot(ev), EventTags(ev.Slug), ct);
    }

    private static string[] EventTags(string slug) => [CacheTags.Events, CacheTags.Event(slug), CacheTags.Home];

    private async Task ApplyAsync(Event ev, EventInput input, CancellationToken ct)
    {
        if (!Slugs.IsValid(input.Slug)) throw new ContentValidationException("Slug", "Validation_SlugFormat");
        if (await db.Events.IgnoreQueryFilters().AnyAsync(e => e.Slug == input.Slug && e.Id != ev.Id, ct))
            throw new ContentValidationException("Slug", "Validation_SlugTaken", input.Slug);
        if (!await db.Cities.AnyAsync(c => c.Id == input.CityId, ct)) throw new ContentValidationException("CityId", "Validation_City");
        if (input.DateEnd is { } end && end < input.DateStart) throw new ContentValidationException("DateEnd", "Validation_DateEnd");
        if (input.Capacity is { } cap && cap <= 0) throw new ContentValidationException("Capacity", "Validation_Capacity");
        if (input.RegistrationMode == RegistrationMode.External && !PublicText.IsSafeExternalUrl(input.ExternalRegistrationUrl))
            throw new ContentValidationException("ExternalRegistrationUrl", "Validation_ExternalUrl");

        ev.Slug = input.Slug;
        ev.Type = input.Type;
        ev.IsPublished = input.IsPublished;
        ev.Season = Required(input.Season, "Season");
        ev.DateStart = input.DateStart; ev.DateEnd = input.DateEnd; ev.StartTime = input.StartTime;
        ev.CityId = input.CityId;
        ev.TitleEn = Required(input.TitleEn, "TitleEn"); ev.TitleAr = Required(input.TitleAr, "TitleAr");
        ev.VenueEn = Required(input.VenueEn, "VenueEn"); ev.VenueAr = Required(input.VenueAr, "VenueAr");
        ev.DescriptionEn = Required(input.DescriptionEn, "DescriptionEn"); ev.DescriptionAr = Required(input.DescriptionAr, "DescriptionAr");
        ev.SwimDistance = Blank(input.SwimDistance); ev.BikeDistance = Blank(input.BikeDistance); ev.RunDistance = Blank(input.RunDistance);
        ev.Categories = string.Join(',', input.Categories.Select(c => c.Trim()).Where(c => c.Length > 0));
        ev.RegistrationMode = input.RegistrationMode;
        ev.RegistrationOpen = input.RegistrationOpen;
        ev.ExternalRegistrationUrl = input.RegistrationMode == RegistrationMode.External ? input.ExternalRegistrationUrl!.Trim() : null;
        ev.Capacity = input.Capacity;
        ev.HeroImagePath = guard.FilePath(input.HeroImagePath, "HeroImagePath");
        ev.ResultsFilePath = guard.FilePath(input.ResultsFilePath, "ResultsFilePath");

        var keepImages = new HashSet<Guid>();
        foreach (var (image, index) in input.Gallery.Select((g, i) => (g, i)))
        {
            var row = image.Id is { } gid ? ev.Gallery.FirstOrDefault(g => g.Id == gid) : null;
            // db.EventGalleryImages.Add, not ev.Gallery.Add: the row's Id is already a real,
            // non-default Guid (BaseEntity assigns one on construction), so appending it only to the
            // navigation collection of an *already-tracked* event leaves EF's change detection to
            // guess whether it is new — and it guesses wrong, tracking it Unchanged/Modified instead
            // of Added, which sends an UPDATE for a row that was never inserted (see ContentService.Apply).
            if (row is null) { row = new EventGalleryImage { EventId = ev.Id, Path = "" }; db.EventGalleryImages.Add(row); }
            keepImages.Add(row.Id);
            row.Path = guard.FilePath(image.Path, "Gallery") ?? throw new ContentValidationException("Gallery", "Validation_GalleryFile");
            row.AltEn = Blank(image.AltEn); row.AltAr = Blank(image.AltAr);
            row.SortOrder = index + 1;
        }
        db.EventGalleryImages.RemoveRange(ev.Gallery.Where(g => !keepImages.Contains(g.Id)).ToList());

        var keepResults = new HashSet<Guid>();
        foreach (var result in input.Results)
        {
            var row = result.Id is { } rid ? ev.Results.FirstOrDefault(r => r.Id == rid) : null;
            // Same reasoning as the gallery loop above: add through the DbSet, not the navigation.
            if (row is null) { row = new EventResult { EventId = ev.Id, AthleteEn = "", AthleteAr = "", Time = "" }; db.EventResults.Add(row); }
            keepResults.Add(row.Id);
            row.Position = result.Position;
            row.AthleteEn = Required(result.AthleteEn, "Results"); row.AthleteAr = Required(result.AthleteAr, "Results");
            row.ClubEn = Blank(result.ClubEn); row.ClubAr = Blank(result.ClubAr);
            row.Time = Required(result.Time, "Results");
        }
        db.EventResults.RemoveRange(ev.Results.Where(r => !keepResults.Contains(r.Id)).ToList());
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new ContentValidationException(field, "Validation_Required") : value.Trim();

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ------------------------------------------------------------------ cities

    public async Task<IReadOnlyList<City>> CitiesForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.Cities.IgnoreQueryFilters().Where(c => c.DeletedAt != null) : db.Cities).AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);

    public async Task<City> SaveCityAsync(CityInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!Slugs.IsValid(input.Key)) throw new ContentValidationException("Key", "Validation_SlugFormat");
        if (await db.Cities.IgnoreQueryFilters().AnyAsync(c => c.Key == input.Key && c.Id != input.Id, ct))
            throw new ContentValidationException("Key", "Validation_SlugTaken", input.Key);

        var city = input.Id is { } id
            ? await db.Cities.SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound")
            : null;
        var before = city is null ? null : Audit.Snapshot(city);
        if (city is null) { city = new City { Key = input.Key, NameEn = "", NameAr = "" }; db.Cities.Add(city); }
        city.Key = input.Key; city.NameEn = Required(input.NameEn, "NameEn"); city.NameAr = Required(input.NameAr, "NameAr");
        city.SvgX = input.SvgX; city.SvgY = input.SvgY; city.LabelAtEnd = input.LabelAtEnd; city.LabelDy = input.LabelDy; city.SortOrder = input.SortOrder;
        await commit.ApplyAsync("City", city.Id, before is null ? "create" : "update", before, Audit.Snapshot(city), [CacheTags.Events], ct);
        return city;
    }

    public async Task DeleteCityAsync(Guid id, CancellationToken ct)
    {
        var city = await db.Cities.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (city is null) return;
        if (await db.Events.IgnoreQueryFilters().AnyAsync(e => e.CityId == id, ct))
            throw new ContentValidationException("Id", "Validation_CityHasEvents");
        db.Cities.Remove(city);
        await commit.ApplyAsync("City", id, "delete", Audit.Snapshot(city), null, [CacheTags.Events], ct);
    }

    public async Task RestoreCityAsync(Guid id, CancellationToken ct)
    {
        var city = await db.Cities.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == id && c.DeletedAt != null, ct);
        if (city is null) return;
        city.DeletedAt = null;
        await commit.ApplyAsync("City", id, "restore", null, Audit.Snapshot(city), [CacheTags.Events], ct);
    }
}
