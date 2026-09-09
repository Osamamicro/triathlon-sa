using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Services;

/// <summary>
/// The newsroom. The read side lists a post once it is published and its date has arrived in
/// Riyadh — the same clock the calendar keeps, so an article and an event never disagree about
/// what day it is. The write side is one activity-log row per save under <see cref="CacheTags.News"/>.
/// </summary>
public sealed class NewsService(AppDbContext db, TimeProvider clock, ContentGuard guard, ContentCommit commit)
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

    // ------------------------------------------------------------------- write

    public async Task<IReadOnlyList<NewsPost>> AllForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.NewsPosts.IgnoreQueryFilters().Where(p => p.DeletedAt != null) : db.NewsPosts)
            .AsNoTracking().OrderByDescending(p => p.PublishedOn).ThenByDescending(p => p.Id).ToListAsync(ct);

    public Task<NewsPost?> ForEditAsync(Guid id, CancellationToken ct) => db.NewsPosts.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<NewsPost> SaveAsync(NewsPostInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!Slugs.IsValid(input.Slug)) throw new ContentValidationException("Slug", "Validation_SlugFormat");
        if (await db.NewsPosts.IgnoreQueryFilters().AnyAsync(p => p.Slug == input.Slug && p.Id != input.Id, ct))
            throw new ContentValidationException("Slug", "Validation_SlugTaken", input.Slug);

        // Validated into locals before anything is touched: a refused save must not leave a half-set
        // post or an unaudited Added row behind in the scope's DbContext for the next save to flush.
        var bodyEn = guard.Html(input.BodyEn) ?? throw new ContentValidationException("BodyEn", "Validation_Required");
        var bodyAr = guard.Html(input.BodyAr) ?? throw new ContentValidationException("BodyAr", "Validation_Required");
        var heroImagePath = FieldLength.Check(guard.FilePath(input.HeroImagePath, "HeroImagePath"), 512, "HeroImagePath");
        // Title and Summary were only ever .Trim()med, never required — a blank post saved silently
        // until this Required check was added (Week 4 final-review finding, same class as ContentService.Apply).
        var titleEn = FieldLength.Check(Required(input.TitleEn, "TitleEn"), 256, "TitleEn")!;
        var titleAr = FieldLength.Check(Required(input.TitleAr, "TitleAr"), 256, "TitleAr")!;
        var summaryEn = FieldLength.Check(Required(input.SummaryEn, "SummaryEn"), 1024, "SummaryEn")!;
        var summaryAr = FieldLength.Check(Required(input.SummaryAr, "SummaryAr"), 1024, "SummaryAr")!;

        var post = input.Id is { } id
            ? await db.NewsPosts.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound")
            : null;
        var before = post is null ? null : Audit.Snapshot(post);
        if (post is null)
        {
            post = new NewsPost { Slug = input.Slug, TitleEn = "", TitleAr = "", SummaryEn = "", SummaryAr = "", BodyEn = "", BodyAr = "" };
            db.NewsPosts.Add(post);
        }

        post.Slug = input.Slug;
        post.TitleEn = titleEn; post.TitleAr = titleAr;
        post.SummaryEn = summaryEn; post.SummaryAr = summaryAr;
        post.BodyEn = bodyEn;
        post.BodyAr = bodyAr;
        post.HeroImagePath = heroImagePath;
        post.PublishedOn = input.PublishedOn;
        post.IsPublished = input.IsPublished;
        await commit.ApplyAsync("NewsPost", post.Id, before is null ? "create" : "update", before, Audit.Snapshot(post), [CacheTags.News], ct);
        return post;
    }

    public async Task SetPublishedAsync(Guid id, bool published, CancellationToken ct)
    {
        var post = await db.NewsPosts.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound");
        var before = Audit.Snapshot(post);
        post.IsPublished = published;
        await commit.ApplyAsync("NewsPost", id, published ? "publish" : "unpublish", before, Audit.Snapshot(post), [CacheTags.News], ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var post = await db.NewsPosts.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return;
        db.NewsPosts.Remove(post);
        await commit.ApplyAsync("NewsPost", id, "delete", Audit.Snapshot(post), null, [CacheTags.News], ct);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct)
    {
        var post = await db.NewsPosts.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt != null, ct);
        if (post is null) return;
        post.DeletedAt = null;
        await commit.ApplyAsync("NewsPost", id, "restore", null, Audit.Snapshot(post), [CacheTags.News], ct);
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new ContentValidationException(field, "Validation_Required") : value.Trim();
}
