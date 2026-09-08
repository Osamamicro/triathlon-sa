using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Areas.Public;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Crm;

namespace Triathlon.Web.Services;

/// <summary>
/// The CMS side of the public site: editable pages and their blocks, the navigation the header and
/// footer render, and the two small lists — committees and clubs — that pages embed.
/// </summary>
public sealed class ContentService(AppDbContext db, ContentGuard guard, ContentCommit commit)
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

    // ------------------------------------------------------------------ pages

    public async Task<IReadOnlyList<Page>> PagesAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.Pages.IgnoreQueryFilters().Where(p => p.DeletedAt != null) : db.Pages)
            .AsNoTracking().OrderBy(p => p.Slug).ToListAsync(ct);

    public Task<Page?> PageForEditAsync(Guid id, CancellationToken ct) =>
        db.Pages.Include(p => p.Blocks.OrderBy(b => b.SortOrder)).SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Page> CreatePageAsync(PageInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        await ValidatePageSlugAsync(input.Slug, existingId: null, ct);

        var page = new Page { Slug = input.Slug, TitleEn = input.TitleEn, TitleAr = input.TitleAr };
        Apply(page, input);
        db.Pages.Add(page);
        await commit.ApplyAsync("Page", page.Id, "create", null, Audit.Snapshot(page), PageTags(page.Slug), ct);
        return page;
    }

    public async Task UpdatePageAsync(Guid id, PageInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var page = await PageForEditAsync(id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound");
        await ValidatePageSlugAsync(input.Slug, id, ct);

        var before = Audit.Snapshot(page);
        var oldSlug = page.Slug;
        Apply(page, input);

        var tags = PageTags(page.Slug).Concat(PageTags(oldSlug)).Distinct().ToArray();
        await commit.ApplyAsync("Page", page.Id, "update", before, Audit.Snapshot(page), tags, ct);
    }

    public async Task SetPagePublishedAsync(Guid id, bool published, CancellationToken ct)
    {
        var page = await db.Pages.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound");
        var before = Audit.Snapshot(page);
        page.IsPublished = published;
        await commit.ApplyAsync("Page", page.Id, published ? "publish" : "unpublish", before, Audit.Snapshot(page), PageTags(page.Slug), ct);
    }

    public async Task DeletePageAsync(Guid id, CancellationToken ct)
    {
        var page = await PageForEditAsync(id, ct);
        if (page is null) return;
        db.PageBlocks.RemoveRange(page.Blocks);
        db.Pages.Remove(page);
        await commit.ApplyAsync("Page", page.Id, "delete", Audit.Snapshot(page), null, PageTags(page.Slug), ct);
    }

    public async Task RestorePageAsync(Guid id, CancellationToken ct)
    {
        var page = await db.Pages.IgnoreQueryFilters().Include(p => p.Blocks).SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt != null, ct);
        if (page is null) return;
        Restore.Aggregate(page, page.Blocks);
        await commit.ApplyAsync("Page", page.Id, "restore", null, Audit.Snapshot(page), PageTags(page.Slug), ct);
    }

    private static string[] PageTags(string slug) =>
        slug == "home" ? [CacheTags.Page(slug), CacheTags.Home] : [CacheTags.Page(slug)];

    private async Task ValidatePageSlugAsync(string slug, Guid? existingId, CancellationToken ct)
    {
        if (!Slugs.IsValid(slug))
            throw new ContentValidationException("Slug", "Validation_SlugFormat");
        if (PublicSite.ReservedSlugs.Contains(slug) && !PublicSite.CompanionPageSlugs.Contains(slug))
            throw new ContentValidationException("Slug", "Validation_SlugReserved", slug);
        if (await db.Pages.IgnoreQueryFilters().AnyAsync(p => p.Slug == slug && p.Id != existingId, ct))
            throw new ContentValidationException("Slug", "Validation_SlugTaken", slug);
    }

    private void Apply(Page page, PageInput input)
    {
        page.Slug = input.Slug;
        page.TitleEn = input.TitleEn.Trim();
        page.TitleAr = input.TitleAr.Trim();
        page.MetaDescriptionEn = Blank(input.MetaDescriptionEn);
        page.MetaDescriptionAr = Blank(input.MetaDescriptionAr);
        page.IsPublished = input.IsPublished;

        var seenBlockIds = new HashSet<Guid>();
        foreach (var blockInput in input.Blocks)
        {
            if (blockInput.Id is { } dupId && !seenBlockIds.Add(dupId))
                throw new ContentValidationException("Blocks", "Validation_DuplicateRow");
        }

        var keep = new HashSet<Guid>();
        foreach (var (blockInput, index) in input.Blocks.Select((b, i) => (b, i)))
        {
            var block = blockInput.Id is { } bid ? page.Blocks.FirstOrDefault(b => b.Id == bid) : null;
            if (block is null)
            {
                // db.PageBlocks.Add, not page.Blocks.Add: the block's Id is already a real, non-default
                // Guid (BaseEntity assigns one on construction), so appending it only to the navigation
                // collection of an *already-tracked* page leaves EF's change detection to guess whether
                // it is new — and it guesses wrong, tracking it Unchanged/Modified instead of Added, which
                // sends an UPDATE for a row that was never inserted. Adding it to the set directly marks
                // it Added unambiguously; relationship fixup still puts it in page.Blocks for the rest of
                // this method to see.
                block = new PageBlock { PageId = page.Id };
                db.PageBlocks.Add(block);
            }

            keep.Add(block.Id);
            block.SortOrder = index + 1;
            block.Type = blockInput.Type;
            block.Variant = Blank(blockInput.Variant);
            block.Anchor = Blank(blockInput.Anchor);
            block.EyebrowEn = Blank(blockInput.EyebrowEn);
            block.EyebrowAr = Blank(blockInput.EyebrowAr);
            // The home hero's title is rendered with Html.Raw (it carries <span class="grad"> and <br>).
            block.TitleEn = guard.Html(blockInput.TitleEn);
            block.TitleAr = guard.Html(blockInput.TitleAr);
            block.BodyEn = guard.Html(blockInput.BodyEn);
            block.BodyAr = guard.Html(blockInput.BodyAr);
            block.ItemsJson = BlockItem.Serialize(blockInput.Items.Select(item => item with
            {
                BodyEn = guard.Html(item.BodyEn), BodyAr = guard.Html(item.BodyAr),
                Href = SafeHref(item.Href),
            }));
            block.CtaLabelEn = Blank(blockInput.CtaLabelEn);
            block.CtaLabelAr = Blank(blockInput.CtaLabelAr);
            block.CtaHref = SafeHref(blockInput.CtaHref);
            block.SecondaryLabelEn = Blank(blockInput.SecondaryLabelEn);
            block.SecondaryLabelAr = Blank(blockInput.SecondaryLabelAr);
            block.SecondaryHref = SafeHref(blockInput.SecondaryHref);
        }

        foreach (var removed in page.Blocks.Where(b => !keep.Contains(b.Id)).ToList())
        {
            db.PageBlocks.Remove(removed);
        }
    }

    /// <summary>An href an editor typed: a site path, a fragment, mailto/tel, or an absolute http(s) URL. Anything else is dropped.</summary>
    internal static string? SafeHref(string? href)
    {
        var value = Blank(href);
        if (value is null) return null;
        if (value.StartsWith('#') || value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) return value;
        if (value.Contains(':', StringComparison.Ordinal))
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) ? value : null;
        // "//host" is protocol-relative and "\\host" is browser-normalised to the same thing — both
        // would leave the site, so a relative href carrying either is refused rather than stored.
        return value.Contains("//", StringComparison.Ordinal) || value.Contains('\\') ? null : value.TrimStart('/');
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>A plausible email address for a settings value: exactly one '@', no whitespace anywhere.</summary>
    private static bool IsPlausibleEmail(string value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsWhiteSpace) && value.Count(c => c == '@') == 1;

    // ------------------------------------------------------------- navigation

    public async Task<IReadOnlyList<NavItem>> NavigationForEditAsync(NavLocation location, CancellationToken ct) =>
        await db.NavItems.AsNoTracking().Where(n => n.Location == location).OrderBy(n => n.SortOrder).ToListAsync(ct);

    /// <summary>Replaces one location's list: rows keep their ids, order is the list's order, rows left out are deleted.</summary>
    public async Task SaveNavigationAsync(NavLocation location, IReadOnlyList<NavItemInput> items, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(items);
        var existing = await db.NavItems.Where(n => n.Location == location).ToListAsync(ct);
        var before = existing.OrderBy(n => n.SortOrder).Select(Audit.Snapshot).ToList();

        // Every item validated into a prepared (input, href) list before the upsert loop below
        // mutates or adds a single row — a refused save must not leave a half-applied reorder behind.
        var seenIds = new HashSet<Guid>();
        var prepared = new List<(NavItemInput Input, string Href)>(items.Count);
        foreach (var input in items)
        {
            if (input.Id is { } dupId && !seenIds.Add(dupId))
                throw new ContentValidationException("Navigation", "Validation_DuplicateRow");
            if (string.IsNullOrWhiteSpace(input.LabelEn) || string.IsNullOrWhiteSpace(input.LabelAr))
                throw new ContentValidationException("Label", "Validation_LabelBothLanguages");
            var href = SafeHref(input.Href) ?? (input.Href.Trim().Length == 0 ? "" : throw new ContentValidationException("Href", "Validation_Href", input.Href));
            prepared.Add((input, href));
        }

        var keep = new HashSet<Guid>();
        var saved = new List<NavItem>();

        foreach (var ((input, href), index) in prepared.Select((p, n) => (p, n)))
        {
            var row = input.Id is { } id ? existing.FirstOrDefault(n => n.Id == id) : null;
            if (row is null)
            {
                row = new NavItem { Location = location, LabelEn = "", LabelAr = "", Href = "" };
                db.NavItems.Add(row);
            }

            keep.Add(row.Id);
            row.LabelEn = input.LabelEn.Trim();
            row.LabelAr = input.LabelAr.Trim();
            row.Href = href;
            row.SortOrder = index + 1;
            row.IsPublished = input.IsPublished;
            saved.Add(row);
        }

        db.NavItems.RemoveRange(existing.Where(n => !keep.Contains(n.Id)));
        var after = saved.OrderBy(n => n.SortOrder).Select(Audit.Snapshot).ToList();
        await commit.ApplyAsync("Navigation", Guid.Empty, "update:" + location, before, after, [CacheTags.Site], ct);
    }

    // ------------------------------------------------------------- committees

    public async Task<IReadOnlyList<Committee>> CommitteesForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.Committees.IgnoreQueryFilters().Where(c => c.DeletedAt != null) : db.Committees)
            .AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);

    public async Task<Committee> SaveCommitteeAsync(CommitteeInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var row = input.Id is { } id
            ? await db.Committees.SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound")
            : null;
        var before = row is null ? null : Audit.Snapshot(row);
        if (row is null)
        {
            row = new Committee { KindEn = "", KindAr = "", NameEn = "", NameAr = "", DescriptionEn = "", DescriptionAr = "" };
            db.Committees.Add(row);
        }

        row.KindEn = input.KindEn.Trim(); row.KindAr = input.KindAr.Trim();
        row.NameEn = input.NameEn.Trim(); row.NameAr = input.NameAr.Trim();
        row.DescriptionEn = input.DescriptionEn.Trim(); row.DescriptionAr = input.DescriptionAr.Trim();
        row.SortOrder = input.SortOrder; row.IsPublished = input.IsPublished;
        await commit.ApplyAsync("Committee", row.Id, before is null ? "create" : "update", before, Audit.Snapshot(row), [CacheTags.Governance], ct);
        return row;
    }

    public async Task DeleteCommitteeAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Committees.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return;
        db.Committees.Remove(row);
        await commit.ApplyAsync("Committee", id, "delete", Audit.Snapshot(row), null, [CacheTags.Governance], ct);
    }

    public async Task RestoreCommitteeAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Committees.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == id && c.DeletedAt != null, ct);
        if (row is null) return;
        row.DeletedAt = null;
        await commit.ApplyAsync("Committee", id, "restore", null, Audit.Snapshot(row), [CacheTags.Governance], ct);
    }

    // ------------------------------------------------------------------ clubs

    public async Task<IReadOnlyList<Club>> ClubsForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.Clubs.IgnoreQueryFilters().Where(c => c.DeletedAt != null) : db.Clubs)
            .AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);

    public async Task<Club> SaveClubAsync(ClubInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var row = input.Id is { } id
            ? await db.Clubs.SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound")
            : null;
        var before = row is null ? null : Audit.Snapshot(row);
        if (row is null)
        {
            row = new Club { NameEn = "", NameAr = "", CityEn = "", CityAr = "" };
            db.Clubs.Add(row);
        }

        row.NameEn = input.NameEn.Trim(); row.NameAr = input.NameAr.Trim();
        row.CityEn = input.CityEn.Trim(); row.CityAr = input.CityAr.Trim();
        row.IsActive = input.IsActive; row.SortOrder = input.SortOrder;
        await commit.ApplyAsync("Club", row.Id, before is null ? "create" : "update", before, Audit.Snapshot(row), [CacheTags.Clubs], ct);
        return row;
    }

    public async Task DeleteClubAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Clubs.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return;
        db.Clubs.Remove(row);
        await commit.ApplyAsync("Club", id, "delete", Audit.Snapshot(row), null, [CacheTags.Clubs], ct);
    }

    public async Task RestoreClubAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Clubs.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == id && c.DeletedAt != null, ct);
        if (row is null) return;
        row.DeletedAt = null;
        await commit.ApplyAsync("Club", id, "restore", null, Audit.Snapshot(row), [CacheTags.Clubs], ct);
    }

    // --------------------------------------------------------------- settings

    public async Task<IReadOnlyDictionary<string, SiteSetting>> SettingsAsync(CancellationToken ct) =>
        await db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, StringComparer.Ordinal, ct);

    public async Task SaveSettingsAsync(IReadOnlyList<SiteSettingInput> inputs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var rows = await db.SiteSettings.ToListAsync(ct);
        var before = rows.ToDictionary(s => s.Key, s => new { s.ValueEn, s.ValueAr });
        foreach (var input in inputs)
        {
            if (!SettingKeys.All.Contains(input.Key, StringComparer.Ordinal))
                throw new ContentValidationException("Key", "Validation_SettingKey", input.Key);

            if (input.Key is SettingKeys.ContactWebsite or SettingKeys.ContactX)
            {
                if (!PublicText.IsSafeExternalUrl(input.ValueEn) || !PublicText.IsSafeExternalUrl(input.ValueAr))
                    throw new ContentValidationException(input.Key, "Validation_Href", input.ValueEn);
            }
            else if (input.Key == SettingKeys.ContactEmail)
            {
                if (!IsPlausibleEmail(input.ValueEn) || !IsPlausibleEmail(input.ValueAr))
                    throw new ContentValidationException(input.Key, "Validation_Email", input.ValueEn);
            }

            var row = rows.FirstOrDefault(s => s.Key == input.Key);
            if (row is null)
            {
                row = new SiteSetting { Key = input.Key };
                db.SiteSettings.Add(row);
                rows.Add(row);
            }
            row.ValueEn = input.ValueEn.Trim();
            row.ValueAr = input.ValueAr.Trim();
        }
        var after = rows.ToDictionary(s => s.Key, s => new { s.ValueEn, s.ValueAr });
        await commit.ApplyAsync("SiteSetting", Guid.Empty, "update", before, after, [CacheTags.Site], ct);
    }
}
