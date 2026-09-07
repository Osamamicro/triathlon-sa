using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Triathlon.Web.Services;

/// <summary>
/// Writes the responsive WebP renditions that sit behind every image on the public site.
/// </summary>
public static class ImageVariants
{
    /// <summary>Rendition widths, narrowest first: phone, tablet, and a full-bleed desktop hero.</summary>
    public static readonly int[] Widths = [480, 960, 1600];

    /// <summary>The largest side a stored image is allowed to keep, to bound decode cost downstream.</summary>
    private const int MaxSourceEdge = 6000;

    /// <summary>
    /// Renders <paramref name="sourcePath"/> to WebP at every width narrower than the source and
    /// returns the file names written, narrowest first.
    /// <para>
    /// Upscaling is never useful — it costs bytes and adds no detail — so widths at or above the
    /// source width are skipped. An image narrower than every configured width would then get no
    /// rendition at all, which would leave the site with a PNG or JPEG where it expects a WebP, so
    /// that case falls back to exactly one rendition at the source's own width.
    /// </para>
    /// </summary>
    public static async Task<IReadOnlyList<string>> WriteAsync(
        string sourcePath,
        string targetDirectory,
        string baseName,
        CancellationToken ct)
    {
        using var image = await Image.LoadAsync(sourcePath, ct);

        if (image.Width > MaxSourceEdge || image.Height > MaxSourceEdge)
        {
            throw new InvalidDataException(
                $"Image is {image.Width}x{image.Height}; the longest side may not exceed {MaxSourceEdge} pixels.");
        }

        var widths = Widths.Where(width => width < image.Width).ToArray();
        if (widths.Length == 0)
        {
            widths = [image.Width];
        }

        var written = new List<string>(widths.Length);
        var encoder = new WebpEncoder();

        foreach (var width in widths)
        {
            var fileName = $"{baseName}-{width}.webp";

            using var rendition = image.Clone(context => context.Resize(new ResizeOptions
            {
                // Height 0 keeps the source aspect ratio.
                Size = new Size(width, 0),
                Mode = ResizeMode.Max,
            }));

            await rendition.SaveAsync(Path.Combine(targetDirectory, fileName), encoder, ct);
            written.Add(fileName);
        }

        return written;
    }
}
