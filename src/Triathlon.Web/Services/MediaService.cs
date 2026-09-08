using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Domain.Media;

namespace Triathlon.Web.Services;

/// <summary>
/// The dashboard's shared media library: every image and PDF an editor has ever uploaded, addressed
/// by id from a picker on any content screen. A delete only removes the catalog row — the file
/// itself stays on disk, since another document/rule/guide/page/event may still hold its path (there
/// is no foreign key from that side to enforce otherwise) — see <see cref="DeleteAsync"/>.
/// </summary>
public sealed class MediaService(AppDbContext db, IFileStore store, ContentCommit commit)
{
    public async Task<MediaAsset> UploadAsync(
        Stream content, string originalFileName, FileKind kind, string? altEn, string? altAr, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);

        // The store is asked first: nothing is added to the catalog until the bytes themselves have
        // been accepted, so a refused upload never leaves an orphaned row with no file behind it.
        StoredFile stored;
        try
        {
            stored = await store.SaveAsync(content, originalFileName, kind, ct);
        }
        catch (InvalidDataException ex)
        {
            throw new ContentValidationException("File", "Validation_File", ex.Message);
        }

        if (await PathIsTakenAsync(stored.Path, ct))
        {
            throw new ContentValidationException("File", "Validation_DuplicateRow");
        }

        var asset = new MediaAsset
        {
            Kind = kind,
            Path = stored.Path,
            ContentType = stored.ContentType,
            Size = stored.Size,
            VariantsJson = JsonSerializer.Serialize(stored.Variants),
            AltEn = Blank(altEn),
            AltAr = Blank(altAr),
            OriginalFileName = originalFileName,
        };
        db.MediaAssets.Add(asset);

        // No public tag is evicted here: nothing renders from the library until an editor picks this
        // asset into a document, rule, guide, page or event — that save evicts its own tags then.
        await commit.ApplyAsync("MediaAsset", asset.Id, "upload", null, Audit.Snapshot(asset), [], ct);
        return asset;
    }

    public async Task<PagedResult<MediaAsset>> ListAsync(FileKind? kind, int skip, int take, CancellationToken ct)
    {
        // Mirror PageQuery's clamp (Domain/Common/PagedResult.cs): a caller-supplied skip/take must
        // never turn into an unbounded or negative query.
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(skip, 0);

        var q = db.MediaAssets.AsNoTracking().OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).AsQueryable();
        if (kind is { } value)
        {
            q = q.Where(a => a.Kind == value);
        }

        var total = await q.CountAsync(ct);
        var items = await q.Skip(skip).Take(take).ToListAsync(ct);
        return new PagedResult<MediaAsset>(items, total, (skip / take) + 1, take);
    }

    /// <summary>
    /// Whether <paramref name="path"/> already names a catalog row, soft-deleted ones included — the
    /// underlying <see cref="IFileStore"/> addresses every upload by a fresh GUID, so a real
    /// collision should never happen, but <see cref="UploadAsync"/> checks it anyway rather than let
    /// two rows silently share one file. Internal so <c>MediaServiceTests</c> can exercise the check
    /// directly, since forcing the file store itself to reuse a path is not practical from a test.
    /// </summary>
    internal Task<bool> PathIsTakenAsync(string path, CancellationToken ct) =>
        db.MediaAssets.IgnoreQueryFilters().AnyAsync(a => a.Path == path, ct);

    public async Task UpdateAltAsync(Guid id, string? altEn, string? altAr, CancellationToken ct)
    {
        var row = await db.MediaAssets.SingleOrDefaultAsync(a => a.Id == id, ct) ?? throw new ContentValidationException("Id", "Validation_NotFound");
        var before = Audit.Snapshot(row);
        row.AltEn = Blank(altEn);
        row.AltAr = Blank(altAr);
        await commit.ApplyAsync("MediaAsset", id, "update", before, Audit.Snapshot(row), [], ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var row = await db.MediaAssets.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (row is null)
        {
            return;
        }

        // db.MediaAssets.Remove, not a file-system delete: StampInterceptor turns this into a
        // DeletedAt update, and the underlying file on disk is left exactly where it is — see the
        // type's summary.
        db.MediaAssets.Remove(row);
        await commit.ApplyAsync("MediaAsset", id, "delete", Audit.Snapshot(row), null, [], ct);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct)
    {
        var row = await db.MediaAssets.IgnoreQueryFilters().SingleOrDefaultAsync(a => a.Id == id && a.DeletedAt != null, ct);
        if (row is null)
        {
            return;
        }

        row.DeletedAt = null;
        await commit.ApplyAsync("MediaAsset", id, "restore", null, Audit.Snapshot(row), [], ct);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
