using System.Text.Json;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Services;

namespace Triathlon.Web.Domain.Media;

/// <summary>
/// One uploaded file in the shared media library — an image with its derived WebP renditions, or a
/// PDF with none (see <see cref="IFileStore"/>). Referenced by <see cref="Path"/> from wherever an
/// editor picked it (event gallery, news hero, page block, document/rule/guide file); there is no
/// foreign key from the picker's side, so deleting the catalog row never touches what already
/// references the path — see <c>MediaService.DeleteAsync</c>.
/// </summary>
public sealed class MediaAsset : BaseEntity
{
    public FileKind Kind { get; set; }

    /// <summary>Public URL path of the stored original, e.g. <c>/media/2026/03/x.png</c>.</summary>
    public required string Path { get; set; }

    /// <summary>The MIME type sniffed from the bytes by the file store, never from the file name.</summary>
    public required string ContentType { get; set; }

    public long Size { get; set; }

    /// <summary>A serialised list of derived WebP variant paths, narrowest first; see <see cref="Variants"/>.</summary>
    public string VariantsJson { get; set; } = "[]";

    public string? AltEn { get; set; }
    public string? AltAr { get; set; }

    /// <summary>The name the file carried on upload, kept for the picker's display and for downloads.</summary>
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// Parsed on each read rather than cached, the same trade-off as <c>PageBlock.Items</c> — this
    /// list is read once per media-picker request, not hot enough to be worth invalidating.
    /// </summary>
    public IReadOnlyList<string> Variants => JsonSerializer.Deserialize<List<string>>(VariantsJson) ?? [];
}
