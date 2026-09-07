using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Crm;

namespace Triathlon.Web.Services;

/// <summary>
/// The CMS side of the public site: editable pages and their blocks, the navigation the header and
/// footer render, and the two small lists — committees and clubs — that pages embed.
/// </summary>
public sealed class ContentService(AppDbContext db)
{
    /// <summary>One published page with its blocks in the order an editor arranged them.</summary>
    public Task<Page?> PageAsync(string slug, CancellationToken ct) =>
        db.Pages.AsNoTracking()
            .Include(p => p.Blocks.OrderBy(b => b.SortOrder))
            .Where(p => p.IsPublished)
            .SingleOrDefaultAsync(p => p.Slug == slug, ct);

    /// <summary>
    /// The published links of one navigation location. Called once per location on every page
    /// render, which is why the whole response is output-cached under <see cref="CacheTags.Site"/>.
    /// </summary>
    public async Task<IReadOnlyList<NavItem>> NavigationAsync(NavLocation location, CancellationToken ct) =>
        await db.NavItems.AsNoTracking()
            .Where(n => n.Location == location && n.IsPublished)
            .OrderBy(n => n.SortOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Committee>> CommitteesAsync(CancellationToken ct) =>
        await db.Committees.AsNoTracking()
            .Where(c => c.IsPublished)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Club>> ClubsAsync(CancellationToken ct) =>
        await db.Clubs.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

    /// <summary>
    /// Every published CMS page's slug, for the sitemap. The caller excludes the slugs that already
    /// have a dedicated entry of their own (<c>home</c>, <c>join</c>, <c>rules</c>, <c>training</c>,
    /// <c>contact</c>) so a page is never listed twice.
    /// </summary>
    public async Task<IReadOnlyList<string>> PublishedPageSlugsAsync(CancellationToken ct) =>
        await db.Pages.AsNoTracking()
            .Where(p => p.IsPublished)
            .Select(p => p.Slug)
            .ToListAsync(ct);
}
