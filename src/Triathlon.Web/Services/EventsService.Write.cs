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

    /// <summary>
    /// Two passes on purpose: every check runs first, against nothing but locals, before a single
    /// property is assigned or a single child row is added — a refused save must leave the scoped
    /// <see cref="AppDbContext"/> exactly as clean as it found it, since the same context serves the
    /// next save in a Blazor circuit.
    /// </summary>
    private async Task ApplyAsync(Event ev, EventInput input, CancellationToken ct)
    {
        // ---- pass 1: validate only — no property assignment, no db.*.Add, below this point ----
        if (!Slugs.IsValid(input.Slug)) throw new ContentValidationException("Slug", "Validation_SlugFormat");
        if (await db.Events.IgnoreQueryFilters().AnyAsync(e => e.Slug == input.Slug && e.Id != ev.Id, ct))
            throw new ContentValidationException("Slug", "Validation_SlugTaken", input.Slug);
        if (!await db.Cities.AnyAsync(c => c.Id == input.CityId, ct)) throw new ContentValidationException("CityId", "Validation_City");
        if (input.DateEnd is { } end && end < input.DateStart) throw new ContentValidationException("DateEnd", "Validation_DateEnd");
        if (input.Capacity is { } cap && cap <= 0) throw new ContentValidationException("Capacity", "Validation_Capacity");
        if (input.RegistrationMode == RegistrationMode.External && !PublicText.IsSafeExternalUrl(input.ExternalRegistrationUrl))
            throw new ContentValidationException("ExternalRegistrationUrl", "Validation_ExternalUrl");

        var season = Required(input.Season, "Season");
        var titleEn = Required(input.TitleEn, "TitleEn"); var titleAr = Required(input.TitleAr, "TitleAr");
        var venueEn = Required(input.VenueEn, "VenueEn"); var venueAr = Required(input.VenueAr, "VenueAr");
        var descriptionEn = Required(input.DescriptionEn, "DescriptionEn"); var descriptionAr = Required(input.DescriptionAr, "DescriptionAr");
        var heroImagePath = guard.FilePath(input.HeroImagePath, "HeroImagePath");
        var resultsFilePath = guard.FilePath(input.ResultsFilePath, "ResultsFilePath");

        var galleryIds = new HashSet<Guid>();
        var galleryPaths = new List<string>(input.Gallery.Count);
        foreach (var image in input.Gallery)
        {
            if (image.Id is { } gid && !galleryIds.Add(gid))
                throw new ContentValidationException("Gallery", "Validation_DuplicateRow");
            galleryPaths.Add(guard.FilePath(image.Path, "Gallery") ?? throw new ContentValidationException("Gallery", "Validation_GalleryFile"));
        }

        var resultIds = new HashSet<Guid>();
        var validatedResults = new List<(string AthleteEn, string AthleteAr, string Time)>(input.Results.Count);
        foreach (var result in input.Results)
        {
            if (result.Id is { } rid && !resultIds.Add(rid))
                throw new ContentValidationException("Results", "Validation_DuplicateRow");
            validatedResults.Add((Required(result.AthleteEn, "Results"), Required(result.AthleteAr, "Results"), Required(result.Time, "Results")));
        }

        // ---- pass 2: every check above passed — assign and upsert children ----
        ev.Slug = input.Slug;
        ev.Type = input.Type;
        ev.IsPublished = input.IsPublished;
        ev.Season = season;
        ev.DateStart = input.DateStart; ev.DateEnd = input.DateEnd; ev.StartTime = input.StartTime;
        ev.CityId = input.CityId;
        ev.TitleEn = titleEn; ev.TitleAr = titleAr;
        ev.VenueEn = venueEn; ev.VenueAr = venueAr;
        ev.DescriptionEn = descriptionEn; ev.DescriptionAr = descriptionAr;
        ev.SwimDistance = Blank(input.SwimDistance); ev.BikeDistance = Blank(input.BikeDistance); ev.RunDistance = Blank(input.RunDistance);
        ev.Categories = string.Join(',', input.Categories.Select(c => c.Trim()).Where(c => c.Length > 0));
        ev.RegistrationMode = input.RegistrationMode;
        ev.RegistrationOpen = input.RegistrationOpen;
        ev.ExternalRegistrationUrl = input.RegistrationMode == RegistrationMode.External ? input.ExternalRegistrationUrl!.Trim() : null;
        ev.Capacity = input.Capacity;
        ev.HeroImagePath = heroImagePath;
        ev.ResultsFilePath = resultsFilePath;

        var keepImages = new HashSet<Guid>();
        foreach (var ((image, path), index) in input.Gallery.Zip(galleryPaths).Select((g, i) => (g, i)))
        {
            var row = image.Id is { } gid ? ev.Gallery.FirstOrDefault(g => g.Id == gid) : null;
            // db.EventGalleryImages.Add, not ev.Gallery.Add: the row's Id is already a real,
            // non-default Guid (BaseEntity assigns one on construction), so appending it only to the
            // navigation collection of an *already-tracked* event leaves EF's change detection to
            // guess whether it is new — and it guesses wrong, tracking it Unchanged/Modified instead
            // of Added, which sends an UPDATE for a row that was never inserted (see ContentService.Apply).
            if (row is null) { row = new EventGalleryImage { EventId = ev.Id, Path = "" }; db.EventGalleryImages.Add(row); }
            keepImages.Add(row.Id);
            row.Path = path;
            row.AltEn = Blank(image.AltEn); row.AltAr = Blank(image.AltAr);
            row.SortOrder = index + 1;
        }
        db.EventGalleryImages.RemoveRange(ev.Gallery.Where(g => !keepImages.Contains(g.Id)).ToList());

        var keepResults = new HashSet<Guid>();
        foreach (var (result, validated) in input.Results.Zip(validatedResults))
        {
            var row = result.Id is { } rid ? ev.Results.FirstOrDefault(r => r.Id == rid) : null;
            // Same reasoning as the gallery loop above: add through the DbSet, not the navigation.
            if (row is null) { row = new EventResult { EventId = ev.Id, AthleteEn = "", AthleteAr = "", Time = "" }; db.EventResults.Add(row); }
            keepResults.Add(row.Id);
            row.Position = result.Position;
            row.AthleteEn = validated.AthleteEn; row.AthleteAr = validated.AthleteAr;
            row.ClubEn = Blank(result.ClubEn); row.ClubAr = Blank(result.ClubAr);
            row.Time = validated.Time;
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
