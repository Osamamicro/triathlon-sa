using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// The media store is the one place untrusted bytes enter the application, so these cover what it
/// promises: the layout on disk, the derived WebP variants, and the rejections that keep a
/// mislabelled, unrecognised or oversized upload out of the media root.
/// </summary>
public sealed class LocalFileStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "stf-media-tests", Guid.NewGuid().ToString("N"));

    public LocalFileStoreTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Image_is_stored_with_three_webp_variants()
    {
        var store = CreateStore();
        await using var png = Png(2000, 1200);

        var stored = await store.SaveAsync(png, "hero photo.PNG", FileKind.Image);

        Assert.Equal("image/png", stored.ContentType);
        Assert.StartsWith("/media/", stored.Path, StringComparison.Ordinal);
        Assert.EndsWith(".png", stored.Path, StringComparison.Ordinal);
        Assert.Equal(3, stored.Variants.Count);

        // The original plus 480/960/1600 — nothing else, and every returned path is a real file.
        Assert.Equal(4, Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Length);
        foreach (var path in stored.Variants.Prepend(stored.Path))
        {
            Assert.True(File.Exists(OnDisk(path)), $"{path} was returned but not written.");
        }

        foreach (var (variant, width) in stored.Variants.Zip(new[] { 480, 960, 1600 }))
        {
            Assert.EndsWith($"-{width}.webp", variant, StringComparison.Ordinal);
            using var image = await Image.LoadAsync(OnDisk(variant));
            Assert.Equal(width, image.Width);
        }
    }

    [Fact]
    public async Task Image_narrower_than_every_variant_width_is_never_upscaled()
    {
        var store = CreateStore();
        await using var png = Png(300, 200);

        var stored = await store.SaveAsync(png, "badge.png", FileKind.Image);

        // No configured width would produce anything but an upscale, so the store falls back to a
        // single variant at the source width rather than returning an image with no WebP at all.
        var variant = Assert.Single(stored.Variants);
        using var image = await Image.LoadAsync(OnDisk(variant));
        Assert.Equal(300, image.Width);
    }

    [Fact]
    public async Task Pdf_is_stored_without_variants()
    {
        var store = CreateStore();
        await using var pdf = new MemoryStream(PdfBytes(2048));

        var stored = await store.SaveAsync(pdf, "rules.pdf", FileKind.Pdf);

        Assert.Equal("application/pdf", stored.ContentType);
        Assert.EndsWith(".pdf", stored.Path, StringComparison.Ordinal);
        Assert.Empty(stored.Variants);
        Assert.Equal(2048, stored.Size);
        Assert.True(File.Exists(OnDisk(stored.Path)));
    }

    [Fact]
    public async Task Pdf_over_the_size_limit_is_rejected()
    {
        var store = CreateStore();
        await using var pdf = new MemoryStream(PdfBytes(26 * 1024 * 1024));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            store.SaveAsync(pdf, "huge.pdf", FileKind.Pdf));

        // A rejected upload must not leave a partial file behind.
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Image_bytes_declared_as_a_pdf_are_rejected()
    {
        var store = CreateStore();
        await using var png = Png(20, 20);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            store.SaveAsync(png, "rules.pdf", FileKind.Pdf));

        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Bytes_of_no_recognised_type_are_rejected()
    {
        var store = CreateStore();
        await using var text = new MemoryStream("<?php system($_GET[0]); ?>"u8.ToArray());

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            store.SaveAsync(text, "shell.png", FileKind.Image));
    }

    [Fact]
    public async Task Stored_path_carries_the_current_year_and_month()
    {
        var store = CreateStore(new FakeTimeProvider(new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero)));
        await using var pdf = new MemoryStream(PdfBytes(64));

        var stored = await store.SaveAsync(pdf, "x.pdf", FileKind.Pdf);

        Assert.StartsWith("/media/2026/03/", stored.Path, StringComparison.Ordinal);
    }

    private LocalFileStore CreateStore(TimeProvider? clock = null) => new(
        Options.Create(new MediaOptions { Root = _root, PublicPrefix = "/media" }),
        new StubEnvironment(_root),
        clock ?? TimeProvider.System);

    /// <summary>Maps a returned public path such as "/media/2026/03/x.png" back onto the temp root.</summary>
    private string OnDisk(string publicPath) =>
        Path.Combine(_root, publicPath["/media/".Length..].Replace('/', Path.DirectorySeparatorChar));

    private static MemoryStream Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }

    /// <summary>A byte array that sniffs as a PDF and is exactly <paramref name="length"/> long.</summary>
    private static byte[] PdfBytes(int length)
    {
        var bytes = new byte[length];
        "%PDF-1.4\n"u8.CopyTo(bytes);
        return bytes;
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Triathlon.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public string WebRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }
}
