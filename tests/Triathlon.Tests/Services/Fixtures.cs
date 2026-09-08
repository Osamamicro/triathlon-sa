using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Triathlon.Tests.Services;

/// <summary>Small fixture builders shared by every test that exercises a file upload.</summary>
internal static class Fixtures
{
    /// <summary>A blank PNG of exactly the given pixel size, positioned at the start of the stream.</summary>
    public static MemoryStream Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }
}
