using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;

namespace Triathlon.Web.Services;

/// <summary>
/// Queries, timeline data and guest registration for the events domain. Split across partial-class
/// files as later sub-tasks add to it; this file owns the clock and the delete cascade.
/// </summary>
public sealed partial class EventsService(AppDbContext db, TimeProvider clock)
{
    /// <summary>Saudi Arabia is UTC+3 year-round — no daylight saving to account for.</summary>
    public static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    /// <summary>Today's date in Riyadh, the only clock the calendar knows.</summary>
    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(RiyadhOffset).DateTime);

    /// <summary>
    /// Soft-deletes the event and its owned children (gallery, results) in one save. Registrations
    /// are CRM records and stay — see docs/adr/0001-soft-delete-cascade.md.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var ev = await db.Events.Include(e => e.Gallery).Include(e => e.Results).SingleOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null) return;
        db.EventGalleryImages.RemoveRange(ev.Gallery);
        db.EventResults.RemoveRange(ev.Results);
        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
    }
}
