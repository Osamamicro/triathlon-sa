using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// <c>MapStaticAssets()</c> only serves the build-time manifest, so it never sees a file an upload
/// wrote at runtime. These prove the application itself — not just nginx in front of production —
/// can serve an upload back, with the nosniff header attached.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class MediaFilesTests(WebAppFixture app) : IDisposable
{
    private readonly string _mediaRoot = Path.Combine(
        Path.GetTempPath(), "stf-media-web-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_mediaRoot))
        {
            Directory.Delete(_mediaRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Uploaded_original_is_served_with_its_content_type_and_nosniff()
    {
        using var site = MediaSite();
        using var client = site.CreateClient();
        var fileStore = site.Services.GetRequiredService<IFileStore>();

        await using var png = Png(800, 600);
        var stored = await fileStore.SaveAsync(png, "hero.png", FileKind.Image);

        using var response = await client.GetAsync(stored.Path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var values));
        Assert.Equal("nosniff", Assert.Single(values));
    }

    [Fact]
    public async Task A_derived_webp_variant_is_served_with_the_webp_content_type()
    {
        using var site = MediaSite();
        using var client = site.CreateClient();
        var fileStore = site.Services.GetRequiredService<IFileStore>();

        await using var png = Png(800, 600);
        var stored = await fileStore.SaveAsync(png, "hero.png", FileKind.Image);
        var variant = stored.Variants[0];

        using var response = await client.GetAsync(variant);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/webp", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>The same application, rooted at a throwaway media directory for this test only.</summary>
    private WebApplicationFactory<Program> MediaSite() =>
        app.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Media:Root"] = _mediaRoot,
            })));

    private static MemoryStream Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }
}
