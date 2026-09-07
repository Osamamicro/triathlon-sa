using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Services;

/// <summary>Category and year counts over published documents, for the library's filter chips.</summary>
public sealed record DocumentFacets(IReadOnlyDictionary<DocumentCategory, int> Categories, IReadOnlyDictionary<int, int> Years);

/// <summary>Which download counter and file table <see cref="DocumentsService.RecordDownloadAsync"/> touches.</summary>
public enum DownloadKind { Document, Rule, Guide }

/// <summary>Queries and download bookkeeping for the documents library, rules and training guides.</summary>
public sealed class DocumentsService(AppDbContext db)
{
    private IQueryable<Document> PublishedDocuments() => db.Documents.AsNoTracking().Where(d => d.IsPublished);

    public async Task<IReadOnlyList<Document>> QueryAsync(DocumentCategory? category, int? year, CancellationToken ct)
    {
        var q = PublishedDocuments();
        if (category is not null) q = q.Where(d => d.Category == category);
        if (year is not null) q = q.Where(d => d.Year == year);
        return await q.OrderByDescending(d => d.Year).ThenBy(d => d.SortOrder).ToListAsync(ct);
    }

    public async Task<DocumentFacets> FacetsAsync(CancellationToken ct)
    {
        var categories = await PublishedDocuments()
            .GroupBy(d => d.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var years = await PublishedDocuments()
            .GroupBy(d => d.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new DocumentFacets(
            categories.ToDictionary(c => c.Category, c => c.Count),
            years.ToDictionary(y => y.Year, y => y.Count));
    }

    /// <summary>
    /// Matched in memory, not translated to SQL: <see cref="RuleAudience"/> is stored as its
    /// comma-separated <c>ToString()</c>, and a rule matches when any bit of the requested audience
    /// overlaps its own — not only on an exact match.
    /// </summary>
    public async Task<IReadOnlyList<RuleOrGuide>> RulesAsync(RuleAudience? audience, CancellationToken ct)
    {
        var rules = await db.Rules.AsNoTracking().Where(r => r.IsPublished).OrderBy(r => r.SortOrder).ToListAsync(ct);
        return audience is null ? rules : rules.Where(r => (r.Audience & audience.Value) != RuleAudience.None).ToList();
    }

    /// <summary>All guides, published ones first, so an "in preparation" card still lists on the page.</summary>
    public async Task<IReadOnlyList<TrainingGuide>> GuidesAsync(CancellationToken ct) =>
        await db.TrainingGuides.AsNoTracking()
            .OrderByDescending(g => g.IsPublished).ThenBy(g => g.SortOrder)
            .ToListAsync(ct);

    public Task<TrainingGuide?> GuideBySlugAsync(string slug, CancellationToken ct) =>
        db.TrainingGuides.AsNoTracking()
            .Include(g => g.Chapters.OrderBy(c => c.SortOrder))
            .Where(g => g.IsPublished)
            .SingleOrDefaultAsync(g => g.Slug == slug, ct);

    /// <summary>Increments the download counter and returns the file's public path, or null when the
    /// id does not name a published item with a file.</summary>
    public async Task<string?> RecordDownloadAsync(DownloadKind kind, Guid id, CancellationToken ct)
    {
        switch (kind)
        {
            case DownloadKind.Document:
            {
                var path = await db.Documents.Where(x => x.Id == id && x.IsPublished).Select(x => x.FilePath).SingleOrDefaultAsync(ct);
                if (path is null) return null;
                await db.Documents.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Downloads, x => x.Downloads + 1), ct);
                return path;
            }
            case DownloadKind.Rule:
            {
                var path = await db.Rules.Where(x => x.Id == id && x.IsPublished).Select(x => x.FilePath).SingleOrDefaultAsync(ct);
                if (path is null) return null;
                await db.Rules.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Downloads, x => x.Downloads + 1), ct);
                return path;
            }
            case DownloadKind.Guide:
            {
                var path = await db.TrainingGuides.Where(x => x.Id == id && x.IsPublished).Select(x => x.FilePath).SingleOrDefaultAsync(ct);
                if (path is null) return null;
                await db.TrainingGuides.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Downloads, x => x.Downloads + 1), ct);
                return path;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}
