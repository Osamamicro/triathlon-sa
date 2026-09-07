using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Services;

/// <summary>
/// The newsroom's read side. A post is live once it is published and its date has arrived in
/// Riyadh — the same clock the calendar keeps, so an article and an event never disagree about
/// what day it is.
/// </summary>
public sealed class NewsService(AppDbContext db, TimeProvider clock)
{
    /// <summary>Today's date in Riyadh; a post dated after it is scheduled, not published.</summary>
    public DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(EventsService.RiyadhOffset).DateTime);

    private IQueryable<NewsPost> Live()
    {
        var today = Today;
        return db.NewsPosts.AsNoTracking()
            .Where(p => p.IsPublished && p.PublishedOn <= today)
            .OrderByDescending(p => p.PublishedOn).ThenByDescending(p => p.Id);
    }

    public async Task<IReadOnlyList<NewsPost>> LatestAsync(int take, CancellationToken ct) =>
        await (take > 0 ? Live().Take(take) : Live()).ToListAsync(ct);

    public async Task<IReadOnlyList<NewsPost>> AllAsync(CancellationToken ct) => await Live().ToListAsync(ct);

    public Task<NewsPost?> BySlugAsync(string slug, CancellationToken ct) =>
        Live().SingleOrDefaultAsync(p => p.Slug == slug, ct);
}
