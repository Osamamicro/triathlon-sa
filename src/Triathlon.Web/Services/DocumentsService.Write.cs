using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Services;

/// <summary>The dashboard's write side for the library: documents, rules and training guides with their chapters.</summary>
public sealed partial class DocumentsService
{
    // ------------------------------------------------------------------ documents

    public async Task<IReadOnlyList<Document>> DocumentsForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.Documents.IgnoreQueryFilters().Where(d => d.DeletedAt != null) : db.Documents)
            .AsNoTracking().OrderByDescending(d => d.Year).ThenBy(d => d.SortOrder).ToListAsync(ct);

    public async Task<Document> SaveDocumentAsync(DocumentInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);

        // ---- pass 1: validate only — no property assignment, no db.*.Add, below this point ----
        var titleEn = Required(input.TitleEn, "TitleEn");
        var titleAr = Required(input.TitleAr, "TitleAr");
        var filePath = guard.FilePath(Required(input.FilePath, "FilePath"))!;
        if (input.FileSize < 0) throw new ContentValidationException("FileSize", "Validation_FileSize");

        var row = input.Id is { } id
            ? await db.Documents.SingleOrDefaultAsync(d => d.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound")
            : null;

        // ---- pass 2: every check above passed — assign ----
        var before = row is null ? null : Audit.Snapshot(row);
        if (row is null)
        {
            row = new Document { TitleEn = "", TitleAr = "", FilePath = "" };
            db.Documents.Add(row);
        }

        row.TitleEn = titleEn; row.TitleAr = titleAr;
        row.Category = input.Category; row.Year = input.Year;
        row.FilePath = filePath; row.FileSize = input.FileSize;
        row.IsPublished = input.IsPublished; row.SortOrder = input.SortOrder;

        await commit.ApplyAsync("Document", row.Id, before is null ? "create" : "update", before, Audit.Snapshot(row), DocumentTags, ct);
        return row;
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Documents.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (row is null) return;
        db.Documents.Remove(row);
        await commit.ApplyAsync("Document", id, "delete", Audit.Snapshot(row), null, DocumentTags, ct);
    }

    public async Task RestoreDocumentAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Documents.IgnoreQueryFilters().SingleOrDefaultAsync(d => d.Id == id && d.DeletedAt != null, ct);
        if (row is null) return;
        row.DeletedAt = null;
        await commit.ApplyAsync("Document", id, "restore", null, Audit.Snapshot(row), DocumentTags, ct);
    }

    private static readonly string[] DocumentTags = [CacheTags.Documents, CacheTags.Governance];

    // ------------------------------------------------------------------ rules

    public async Task<IReadOnlyList<RuleOrGuide>> RulesForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.Rules.IgnoreQueryFilters().Where(r => r.DeletedAt != null) : db.Rules)
            .AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(ct);

    public async Task<RuleOrGuide> SaveRuleAsync(RuleInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);

        // ---- pass 1: validate only — no property assignment, no db.*.Add, below this point ----
        if (!Slugs.IsValid(input.Slug)) throw new ContentValidationException("Slug", "Validation_SlugFormat");
        if (await db.Rules.IgnoreQueryFilters().AnyAsync(r => r.Slug == input.Slug && r.Id != input.Id, ct))
            throw new ContentValidationException("Slug", "Validation_SlugTaken", input.Slug);

        var titleEn = Required(input.TitleEn, "TitleEn"); var titleAr = Required(input.TitleAr, "TitleAr");
        var descriptionEn = Required(input.DescriptionEn, "DescriptionEn"); var descriptionAr = Required(input.DescriptionAr, "DescriptionAr");
        var filePath = guard.FilePath(Required(input.FilePath, "FilePath"))!;
        if (input.FileSize < 0) throw new ContentValidationException("FileSize", "Validation_FileSize");

        var row = input.Id is { } id
            ? await db.Rules.SingleOrDefaultAsync(r => r.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound")
            : null;

        // ---- pass 2: every check above passed — assign ----
        var before = row is null ? null : Audit.Snapshot(row);
        if (row is null)
        {
            row = new RuleOrGuide { Slug = input.Slug, TitleEn = "", TitleAr = "", DescriptionEn = "", DescriptionAr = "", FilePath = "" };
            db.Rules.Add(row);
        }

        row.Slug = input.Slug;
        row.TitleEn = titleEn; row.TitleAr = titleAr;
        row.DescriptionEn = descriptionEn; row.DescriptionAr = descriptionAr;
        row.Audience = input.Audience;
        row.FilePath = filePath; row.FileSize = input.FileSize;
        row.UpdatedOn = input.UpdatedOn;
        row.IsPublished = input.IsPublished; row.SortOrder = input.SortOrder;

        await commit.ApplyAsync("Rule", row.Id, before is null ? "create" : "update", before, Audit.Snapshot(row), [CacheTags.Rules], ct);
        return row;
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Rules.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (row is null) return;
        db.Rules.Remove(row);
        await commit.ApplyAsync("Rule", id, "delete", Audit.Snapshot(row), null, [CacheTags.Rules], ct);
    }

    public async Task RestoreRuleAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Rules.IgnoreQueryFilters().SingleOrDefaultAsync(r => r.Id == id && r.DeletedAt != null, ct);
        if (row is null) return;
        row.DeletedAt = null;
        await commit.ApplyAsync("Rule", id, "restore", null, Audit.Snapshot(row), [CacheTags.Rules], ct);
    }

    // ------------------------------------------------------------------ training guides

    public async Task<IReadOnlyList<TrainingGuide>> GuidesForEditAsync(bool deletedOnly, CancellationToken ct) =>
        await (deletedOnly ? db.TrainingGuides.IgnoreQueryFilters().Where(g => g.DeletedAt != null) : db.TrainingGuides)
            .AsNoTracking().OrderBy(g => g.SortOrder).ToListAsync(ct);

    public Task<TrainingGuide?> GuideForEditAsync(Guid id, CancellationToken ct) =>
        db.TrainingGuides.Include(g => g.Chapters.OrderBy(c => c.SortOrder)).SingleOrDefaultAsync(g => g.Id == id, ct);

    /// <summary>
    /// Two passes on purpose, the same shape as <c>EventsService.Write.cs</c>'s <c>ApplyAsync</c>:
    /// every check runs first, against nothing but locals, before a single property is assigned or a
    /// single chapter row is added — a refused save must leave the scoped <see cref="AppDbContext"/>
    /// exactly as clean as it found it.
    /// </summary>
    public async Task<TrainingGuide> SaveGuideAsync(GuideInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);

        // ---- pass 1: validate only — no property assignment, no db.*.Add, below this point ----
        if (!Slugs.IsValid(input.Slug)) throw new ContentValidationException("Slug", "Validation_SlugFormat");
        if (await db.TrainingGuides.IgnoreQueryFilters().AnyAsync(g => g.Slug == input.Slug && g.Id != input.Id, ct))
            throw new ContentValidationException("Slug", "Validation_SlugTaken", input.Slug);
        if (input.FileSize is { } size && size < 0) throw new ContentValidationException("FileSize", "Validation_FileSize");
        var filePath = guard.FilePath(input.FilePath, "FilePath");
        if (input.IsPublished && filePath is null && input.Chapters.Count == 0)
            throw new ContentValidationException("IsPublished", "Validation_GuideNeedsContent");

        var titleEn = Required(input.TitleEn, "TitleEn"); var titleAr = Required(input.TitleAr, "TitleAr");
        var summaryEn = Required(input.SummaryEn, "SummaryEn"); var summaryAr = Required(input.SummaryAr, "SummaryAr");
        var levelEn = Required(input.LevelEn, "LevelEn"); var levelAr = Required(input.LevelAr, "LevelAr");

        var guide = input.Id is { } id
            ? await GuideForEditAsync(id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound")
            : null;

        var chapterIds = new HashSet<Guid>();
        var validatedChapters = new List<(string TitleEn, string TitleAr, string BodyEn, string BodyAr)>(input.Chapters.Count);
        foreach (var chapter in input.Chapters)
        {
            if (chapter.Id is { } cid && !chapterIds.Add(cid))
                throw new ContentValidationException("Chapters", "Validation_DuplicateRow");
            var bodyEn = guard.Html(chapter.BodyEn) ?? throw new ContentValidationException("Chapters", "Validation_Required");
            var bodyAr = guard.Html(chapter.BodyAr) ?? throw new ContentValidationException("Chapters", "Validation_Required");
            validatedChapters.Add((Required(chapter.TitleEn, "Chapters"), Required(chapter.TitleAr, "Chapters"), bodyEn, bodyAr));
        }

        // ---- pass 2: every check above passed — assign and upsert chapters ----
        var before = guide is null ? null : Audit.Snapshot(guide);
        if (guide is null)
        {
            guide = new TrainingGuide { Slug = input.Slug, TitleEn = "", TitleAr = "", SummaryEn = "", SummaryAr = "", LevelEn = "", LevelAr = "" };
            db.TrainingGuides.Add(guide);
        }

        guide.Slug = input.Slug;
        guide.TitleEn = titleEn; guide.TitleAr = titleAr;
        guide.SummaryEn = summaryEn; guide.SummaryAr = summaryAr;
        guide.LevelEn = levelEn; guide.LevelAr = levelAr;
        guide.FilePath = filePath; guide.FileSize = filePath is null ? null : input.FileSize;
        guide.IsPublished = input.IsPublished; guide.SortOrder = input.SortOrder;

        var keep = new HashSet<Guid>();
        foreach (var ((chapterInput, validated), index) in input.Chapters.Zip(validatedChapters).Select((c, i) => (c, i)))
        {
            var row = chapterInput.Id is { } cid ? guide.Chapters.FirstOrDefault(c => c.Id == cid) : null;
            // db.TrainingGuideChapters.Add, not guide.Chapters.Add — see the identical note in
            // EventsService.Write.cs's ApplyAsync: the row's Id is already a real Guid, so appending
            // it only to the navigation collection would leave EF guessing whether it is new.
            if (row is null)
            {
                row = new TrainingGuideChapter { TrainingGuideId = guide.Id, TitleEn = "", TitleAr = "", BodyEn = "", BodyAr = "" };
                db.TrainingGuideChapters.Add(row);
            }

            keep.Add(row.Id);
            row.SortOrder = index + 1;
            row.TitleEn = validated.TitleEn; row.TitleAr = validated.TitleAr;
            row.BodyEn = validated.BodyEn; row.BodyAr = validated.BodyAr;
        }
        db.TrainingGuideChapters.RemoveRange(guide.Chapters.Where(c => !keep.Contains(c.Id)).ToList());

        await commit.ApplyAsync("TrainingGuide", guide.Id, before is null ? "create" : "update", before, Audit.Snapshot(guide), [CacheTags.Guides], ct);
        return guide;
    }

    /// <summary>Soft-deletes the guide and its chapters in one save, stamped with the same instant — see ADR 0001 and <c>EventsService.DeleteAsync</c>.</summary>
    public async Task DeleteGuideAsync(Guid id, CancellationToken ct)
    {
        var guide = await db.TrainingGuides.Include(g => g.Chapters).SingleOrDefaultAsync(g => g.Id == id, ct);
        if (guide is null) return;
        var before = Audit.Snapshot(guide);
        var now = clock.GetUtcNow();
        foreach (var chapter in guide.Chapters) chapter.DeletedAt = now;
        guide.DeletedAt = now;
        await commit.ApplyAsync("TrainingGuide", guide.Id, "delete", before, null, [CacheTags.Guides], ct);
    }

    public async Task RestoreGuideAsync(Guid id, CancellationToken ct)
    {
        var guide = await db.TrainingGuides.IgnoreQueryFilters().Include(g => g.Chapters).SingleOrDefaultAsync(g => g.Id == id && g.DeletedAt != null, ct);
        if (guide is null) return;
        Restore.Aggregate(guide, guide.Chapters);
        await commit.ApplyAsync("TrainingGuide", guide.Id, "restore", null, Audit.Snapshot(guide), [CacheTags.Guides], ct);
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new ContentValidationException(field, "Validation_Required") : value.Trim();
}
