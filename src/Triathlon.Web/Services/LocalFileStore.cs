using System.Buffers;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace Triathlon.Web.Services;

/// <summary>
/// Stores uploads on the local disk under <c>{root}/{yyyy}/{MM}/{guid}.{ext}</c>.
/// <para>
/// The uploaded file name is never trusted: the extension comes from the bytes themselves, and the
/// stored name is a fresh GUID, so a caller cannot choose a path, an extension, or collide with an
/// existing file. Anything that does not sniff as one of the accepted types, or whose type
/// contradicts the declared <see cref="FileKind"/>, is rejected before a byte reaches the disk.
/// </para>
/// </summary>
public sealed class LocalFileStore : IFileStore
{
    /// <summary>Upper bound for any single upload, images included.</summary>
    public const long MaxBytes = 25L * 1024 * 1024;

    /// <summary>Enough leading bytes for every signature below, WebP's 12-byte one included.</summary>
    private const int SniffLength = 16;

    private const int CopyBufferSize = 64 * 1024;

    /// <summary>PNG's eight-byte signature. Written out as bytes because a "\x89…"u8 literal would
    /// encode U+0089 as the two UTF-8 bytes C2 89 rather than the single byte 0x89.</summary>
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];

    private readonly string _root;
    private readonly string _publicPrefix;
    private readonly TimeProvider _clock;

    public LocalFileStore(IOptions<MediaOptions> options, IWebHostEnvironment environment, TimeProvider clock)
    {
        var settings = options.Value;
        _root = ResolveRoot(settings.Root, environment);
        _publicPrefix = "/" + settings.PublicPrefix.Trim('/');
        _clock = clock;
    }

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        FileKind kind,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);

        var header = new byte[SniffLength];
        var headerLength = await content.ReadAtLeastAsync(header, SniffLength, throwOnEndOfStream: false, ct);

        var signature = Sniff(header.AsSpan(0, headerLength))
            ?? throw new InvalidDataException($"'{originalFileName}' is not a file type this site accepts.");

        if (signature.Kind != kind)
        {
            throw new InvalidDataException(
                $"'{originalFileName}' was uploaded as {kind} but its contents are {signature.ContentType}.");
        }

        // Cheap rejection for the common case, so an oversized upload never touches the disk.
        if (content.CanSeek && content.Length > MaxBytes)
        {
            throw TooLarge(originalFileName);
        }

        var now = _clock.GetUtcNow();
        var year = now.Year.ToString("D4", CultureInfo.InvariantCulture);
        var month = now.Month.ToString("D2", CultureInfo.InvariantCulture);

        var directory = Path.Combine(_root, year, month);
        Directory.CreateDirectory(directory);

        var id = Guid.NewGuid().ToString("N");
        var fileName = $"{id}.{signature.Extension}";
        var fullPath = Path.Combine(directory, fileName);

        var written = new List<string> { fullPath };

        try
        {
            var size = await CopyToDiskAsync(content, header.AsMemory(0, headerLength), fullPath, originalFileName, ct);

            IReadOnlyList<string> variants = [];

            if (kind == FileKind.Image)
            {
                var names = await ImageVariants.WriteAsync(fullPath, directory, id, ct);
                written.AddRange(names.Select(name => Path.Combine(directory, name)));
                variants = [.. names.Select(name => PublicPath(year, month, name))];
            }

            return new StoredFile(PublicPath(year, month, fileName), signature.ContentType, size, variants);
        }
        catch
        {
            // A rejected or failed upload must not leave a partial file — or a half-written set of
            // renditions — behind for something else to serve.
            foreach (var path in written)
            {
                TryDelete(path);
            }

            throw;
        }
    }

    private string PublicPath(string year, string month, string fileName) =>
        $"{_publicPrefix}/{year}/{month}/{fileName}";

    private static async Task<long> CopyToDiskAsync(
        Stream source,
        ReadOnlyMemory<byte> header,
        string fullPath,
        string originalFileName,
        CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);

        try
        {
            await using var file = new FileStream(
                fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);

            await file.WriteAsync(header, ct);
            long total = header.Length;

            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, CopyBufferSize), ct)) > 0)
            {
                total += read;

                // Re-checked while copying because a request body stream cannot be measured up front.
                if (total > MaxBytes)
                {
                    throw TooLarge(originalFileName);
                }

                await file.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static InvalidDataException TooLarge(string originalFileName) =>
        new($"'{originalFileName}' is larger than the {MaxBytes / (1024 * 1024)} MB upload limit.");

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best effort: the upload has already failed, and a leftover orphan is not worth
            // masking the real exception for.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Identifies the upload from its leading bytes. A browser or an API caller can claim any MIME
    /// type it likes, so the signature is the only thing this store believes.
    /// </summary>
    private static Signature? Sniff(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(PngSignature))
        {
            return new Signature("image/png", "png", FileKind.Image);
        }

        if (header.StartsWith(JpegSignature))
        {
            return new Signature("image/jpeg", "jpg", FileKind.Image);
        }

        // RIFF container with a WEBP form type; bytes 4-7 are the chunk length and vary.
        if (header.Length >= 12 && header.StartsWith("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            return new Signature("image/webp", "webp", FileKind.Image);
        }

        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
        {
            return new Signature("image/gif", "gif", FileKind.Image);
        }

        if (header.StartsWith("%PDF"u8))
        {
            return new Signature("application/pdf", "pdf", FileKind.Pdf);
        }

        return null;
    }

    private static string ResolveRoot(string configured, IWebHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return Path.Combine(environment.ContentRootPath, "wwwroot", "media");
        }

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
    }

    private sealed record Signature(string ContentType, string Extension, FileKind Kind);
}
