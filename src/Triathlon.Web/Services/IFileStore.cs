namespace Triathlon.Web.Services;

/// <summary>What an upload is allowed to be. The store rejects anything whose bytes disagree.</summary>
public enum FileKind
{
    /// <summary>A raster image: PNG, JPEG, WebP or GIF.</summary>
    Image,

    /// <summary>A PDF document — rules, minutes, forms.</summary>
    Pdf,
}

/// <summary>
/// One stored upload. <paramref name="Path"/> and every entry in <paramref name="Variants"/> are
/// public URL paths ready to put in an <c>src</c> or <c>href</c>, not filesystem paths.
/// </summary>
/// <param name="Path">Public path of the file exactly as uploaded.</param>
/// <param name="ContentType">The MIME type sniffed from the bytes, never from the file name.</param>
/// <param name="Size">Length of the stored original, in bytes.</param>
/// <param name="Variants">Public paths of the derived WebP renditions, narrowest first. Empty for PDFs.</param>
public sealed record StoredFile(string Path, string ContentType, long Size, IReadOnlyList<string> Variants);

/// <summary>
/// Persists an upload and returns where it landed. The only implementation today writes to the
/// local disk; an object-store implementation would slot in behind this interface unchanged.
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// Stores <paramref name="content"/>, deriving image renditions when appropriate.
    /// </summary>
    /// <exception cref="InvalidDataException">
    /// The bytes are not a type the store accepts, do not match <paramref name="kind"/>, or exceed
    /// the size limit. Nothing is left on disk when this is thrown.
    /// </exception>
    Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        FileKind kind,
        CancellationToken ct = default);
}
