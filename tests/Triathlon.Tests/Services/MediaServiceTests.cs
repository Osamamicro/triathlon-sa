using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Media;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// The dashboard's shared media library: an upload lands in the catalog only once the file store has
/// accepted its bytes, a mismatched upload is refused as a validation error rather than an
/// <see cref="InvalidDataException"/> leaking out, and the listing serves newest first.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class MediaServiceTests(WebAppFixture app)
{
    [Fact]
    public async Task Uploading_an_image_stores_it_with_variants()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var media = scope.ServiceProvider.GetRequiredService<MediaService>();
        await using var png = Fixtures.Png(800, 600);

        var asset = await media.UploadAsync(png, "hero photo.PNG", FileKind.Image, "Hero", "البطل", CancellationToken.None);

        Assert.Equal(FileKind.Image, asset.Kind);
        Assert.True(asset.Variants.Count > 0);
        Assert.StartsWith("/media/", asset.Path, StringComparison.Ordinal);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(await db.MediaAssets.SingleOrDefaultAsync(a => a.Id == asset.Id));
    }

    [Fact]
    public async Task An_upload_whose_bytes_do_not_match_the_declared_kind_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var media = scope.ServiceProvider.GetRequiredService<MediaService>();
        await using var text = new MemoryStream(Encoding.UTF8.GetBytes("not actually a pdf"));

        await Assert.ThrowsAsync<ContentValidationException>(() =>
            media.UploadAsync(text, "doc.pdf", FileKind.Pdf, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task A_path_already_in_the_catalog_is_reported_as_taken()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var media = scope.ServiceProvider.GetRequiredService<MediaService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var path = "/media/2026/09/" + Guid.NewGuid().ToString("N") + ".png";
        db.MediaAssets.Add(new MediaAsset { Path = path, ContentType = "image/png", Size = 1, OriginalFileName = "existing.png" });
        await db.SaveChangesAsync(CancellationToken.None);

        Assert.True(await media.PathIsTakenAsync(path, CancellationToken.None));
        Assert.False(await media.PathIsTakenAsync(path + "-not-taken", CancellationToken.None));
    }

    [Fact]
    public async Task Listing_by_kind_returns_the_newest_first()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var media = scope.ServiceProvider.GetRequiredService<MediaService>();
        await using var png = Fixtures.Png(400, 300);
        var asset = await media.UploadAsync(png, "list-test.png", FileKind.Image, null, null, CancellationToken.None);

        var page = await media.ListAsync(FileKind.Image, false, 0, 10, CancellationToken.None);

        Assert.Equal(asset.Id, page.Items[0].Id);
        Assert.Equal(1, page.Page);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public async Task DeletedOnly_lists_a_deleted_asset_and_excludes_it_from_the_live_listing()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var media = scope.ServiceProvider.GetRequiredService<MediaService>();
        await using var png = Fixtures.Png(200, 200);
        var asset = await media.UploadAsync(png, "trash-test.png", FileKind.Image, null, null, CancellationToken.None);

        await media.DeleteAsync(asset.Id, CancellationToken.None);

        var live = await media.ListAsync(FileKind.Image, false, 0, 50, CancellationToken.None);
        Assert.DoesNotContain(live.Items, a => a.Id == asset.Id);

        var trash = await media.ListAsync(FileKind.Image, true, 0, 50, CancellationToken.None);
        Assert.Contains(trash.Items, a => a.Id == asset.Id);
    }
}
