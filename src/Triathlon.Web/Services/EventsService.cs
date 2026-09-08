using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Services;

/// <summary>Cities, events and registration counts for one rendering of the events timeline.</summary>
public sealed record TimelineData(
    string Season,
    IReadOnlyList<string> Seasons,
    IReadOnlyList<City> Cities,          // only cities that have an event in the selection
    IReadOnlyList<Event> Events);        // ordered by DateStart, City loaded

/// <summary>A guest's own entry into an event, posted from the public registration form.</summary>
public sealed record GuestRegistration(string FullName, string Email, string? Phone, string Category, string? Club);

/// <summary>What happened to a guest's attempt to register — see <see cref="EventsService.RegisterAsync"/>.</summary>
public enum RegistrationOutcome { Confirmed, Waitlist, Closed, NotFound }

/// <summary>
/// Queries, timeline data and guest registration for the events domain. Split across partial-class
/// files as later sub-tasks add to it; this file owns the clock and the delete cascade.
/// </summary>
public sealed partial class EventsService(AppDbContext db, TimeProvider clock, ContentGuard guard, ContentCommit commit)
{
    /// <summary>Saudi Arabia is UTC+3 year-round — no daylight saving to account for.</summary>
    public static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    /// <summary>Today's date in Riyadh, the only clock the calendar knows.</summary>
    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(RiyadhOffset).DateTime);

    private IQueryable<Event> Published() => db.Events.AsNoTracking().Include(e => e.City).Where(e => e.IsPublished);

    private static IQueryable<Event> Filter(IQueryable<Event> q, EventType? type, string? cityKey)
    {
        if (type is not null) q = q.Where(e => e.Type == type);
        if (!string.IsNullOrWhiteSpace(cityKey)) q = q.Where(e => e.City.Key == cityKey);
        return q;
    }

    public async Task<IReadOnlyList<Event>> UpcomingAsync(EventType? type, string? cityKey, int take, CancellationToken ct)
    {
        var today = Today;
        var q = Filter(Published(), type, cityKey).Where(e => e.DateStart >= today).OrderBy(e => e.DateStart).ThenBy(e => e.TitleEn);
        return await (take > 0 ? q.Take(take) : q).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Event>> PastAsync(EventType? type, string? cityKey, CancellationToken ct)
    {
        var today = Today;
        return await Filter(Published(), type, cityKey).Where(e => e.DateStart < today)
            .OrderByDescending(e => e.DateStart).ToListAsync(ct);
    }

    public Task<Event?> BySlugAsync(string slug, CancellationToken ct) =>
        Published()
            .Include(e => e.Gallery.OrderBy(g => g.SortOrder))
            .Include(e => e.Results.OrderBy(r => r.Position))
            .SingleOrDefaultAsync(e => e.Slug == slug, ct);

    /// <summary>
    /// Every published event, past and future, in date order — the calendar feed's source. A
    /// subscriber's client keeps the whole series, so the feed is not trimmed to a season the way
    /// the timeline is.
    /// </summary>
    public async Task<IReadOnlyList<Event>> AllPublishedAsync(EventType? type, CancellationToken ct) =>
        await Filter(Published(), type, null).OrderBy(e => e.DateStart).ThenBy(e => e.TitleEn).ToListAsync(ct);

    public async Task<IReadOnlyList<City>> CitiesAsync(CancellationToken ct) =>
        await db.Cities.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> SeasonAsync(string? season, EventType? type, CancellationToken ct)
    {
        season ??= await CurrentSeasonAsync(ct);
        return await Filter(Published(), type, null).Where(e => e.Season == season).OrderBy(e => e.DateStart).ToListAsync(ct);
    }

    /// <summary>The season of the next upcoming event, or the latest season on record.</summary>
    private async Task<string> CurrentSeasonAsync(CancellationToken ct)
    {
        var today = Today;
        var next = await Published().Where(e => e.DateStart >= today).OrderBy(e => e.DateStart).Select(e => e.Season).FirstOrDefaultAsync(ct);
        return next ?? await Published().OrderByDescending(e => e.DateStart).Select(e => e.Season).FirstOrDefaultAsync(ct) ?? "";
    }

    public async Task<TimelineData> TimelineAsync(string? season, EventType? type, CancellationToken ct)
    {
        season ??= await CurrentSeasonAsync(ct);
        var seasons = await Published().Select(e => e.Season).Distinct().ToListAsync(ct);
        var events = await SeasonAsync(season, type, ct);
        var cityIds = events.Select(e => e.CityId).ToHashSet();
        var cities = (await CitiesAsync(ct)).Where(c => cityIds.Contains(c.Id)).ToList();
        return new TimelineData(season, seasons.OrderByDescending(s => s).ToList(), cities, events);
    }

    public async Task<RegistrationOutcome> RegisterAsync(string slug, GuestRegistration form, CancellationToken ct)
    {
        var ev = await db.Events.Where(e => e.IsPublished).SingleOrDefaultAsync(e => e.Slug == slug, ct);
        if (ev is null) return RegistrationOutcome.NotFound;
        if (ev.RegistrationMode != RegistrationMode.Internal || ev.StatusOn(Today) != EventStatus.Open) return RegistrationOutcome.Closed;

        // Capacity is checked against confirmed entries. Two visitors racing for the last place can
        // both be confirmed; the CRM officer resolves that by hand, which is acceptable at this scale.
        var confirmed = await db.EventRegistrations.CountAsync(r => r.EventId == ev.Id && r.Status == RegistrationStatus.Confirmed, ct);
        var status = ev.Capacity is int cap && confirmed >= cap ? RegistrationStatus.Waitlist : RegistrationStatus.Confirmed;

        db.EventRegistrations.Add(new EventRegistration
        {
            EventId = ev.Id, FullName = form.FullName.Trim(), Email = form.Email.Trim(), Phone = form.Phone?.Trim(),
            Category = form.Category.Trim(), Club = string.IsNullOrWhiteSpace(form.Club) ? null : form.Club.Trim(), Status = status,
        });
        await db.SaveChangesAsync(ct);
        return status == RegistrationStatus.Waitlist ? RegistrationOutcome.Waitlist : RegistrationOutcome.Confirmed;
    }

    /// <summary>
    /// Soft-deletes the event and its owned children (gallery, results) in one save. Registrations
    /// are CRM records and stay — see docs/adr/0001-soft-delete-cascade.md.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var ev = await db.Events.Include(e => e.Gallery).Include(e => e.Results).SingleOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null) return;
        var before = Audit.Snapshot(ev);
        // Every row — the event and each child — gets the exact same DeletedAt instant, assigned once
        // here rather than left for StampInterceptor to fill in per-row at save time. ADR 0001's
        // restore only re-attaches a child whose DeletedAt falls within one second of the parent's; a
        // slow save could otherwise let the interceptor stamp the gallery/results a moment later than
        // the event and silently strand them outside that window on restore.
        var now = clock.GetUtcNow();
        foreach (var image in ev.Gallery) image.DeletedAt = now;
        foreach (var result in ev.Results) result.DeletedAt = now;
        // Set DeletedAt directly rather than db.Events.Remove(ev): a registration for this event that
        // is already tracked in this scope (e.g. one just created by RegisterAsync) gets fixed up into
        // ev.Registrations by EF regardless of whether this query loaded it, and Remove()'s in-memory
        // cascade check throws the moment it sees that tracked, required-relationship child — even
        // though the "delete" is only ever a DeletedAt update, never a real row DELETE (StampInterceptor
        // rewrites every hard delete that way). Mutating the column ourselves takes the same UPDATE
        // path without ever putting the event into EntityState.Deleted, so that check never runs. The
        // gallery/results rows above take the same Modified path (StampInterceptor's Modified branch
        // still fills in UpdatedAt/UpdatedBy for all three). Registrations are never touched either way
        // — see docs/adr/0001-soft-delete-cascade.md.
        ev.DeletedAt = now;
        await commit.ApplyAsync("Event", ev.Id, "delete", before, null, EventTags(ev.Slug), ct);
    }
}
