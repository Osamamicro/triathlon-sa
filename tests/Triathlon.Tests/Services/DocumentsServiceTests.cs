using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Services;
using Triathlon.Tests.Web;

namespace Triathlon.Tests.Services;

[Collection(WebAppCollection.Name)]
public sealed class DocumentsServiceTests(WebAppFixture app)
{
    [Fact]
    public async Task Facets_count_published_documents_by_category_and_year()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var facets = await scope.ServiceProvider.GetRequiredService<DocumentsService>().FacetsAsync(CancellationToken.None);

        Assert.Equal(4, facets.Categories[DocumentCategory.Governance]);
        Assert.Equal(2, facets.Categories[DocumentCategory.Finance]);
        Assert.Equal(3, facets.Categories[DocumentCategory.Minutes]);
        Assert.Equal(4, facets.Years[2026]);
        Assert.Equal(4, facets.Years[2025]);
        Assert.Equal(1, facets.Years[2024]);
    }

    [Fact]
    public async Task Query_filters_and_orders_newest_first()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<DocumentsService>();

        var minutes2026 = await service.QueryAsync(DocumentCategory.Minutes, 2026, CancellationToken.None);
        var all = await service.QueryAsync(null, null, CancellationToken.None);

        Assert.Equal(2, minutes2026.Count);
        Assert.Equal(9, all.Count);
        Assert.Equal(2026, all[0].Year);
        Assert.Equal(2024, all[^1].Year);
    }

    [Fact]
    public async Task Rules_filter_by_audience_flag()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<DocumentsService>();

        var officials = await service.RulesAsync(RuleAudience.Officials, CancellationToken.None);
        var all = await service.RulesAsync(null, CancellationToken.None);

        Assert.Equal(5, all.Count);
        Assert.Contains(officials, r => r.Slug == "technical-officials");
        Assert.DoesNotContain(officials, r => r.Slug == "age-group-guide");
    }

    [Fact]
    public async Task Recording_a_download_increments_the_counter_and_returns_the_path()
    {
        Guid id; int before;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var all = await scope.ServiceProvider.GetRequiredService<DocumentsService>().QueryAsync(null, null, CancellationToken.None);
            id = all[0].Id; before = all[0].Downloads;
        }

        string? path;
        await using (var scope = app.Services.CreateAsyncScope())
            path = await scope.ServiceProvider.GetRequiredService<DocumentsService>().RecordDownloadAsync(DownloadKind.Document, id, CancellationToken.None);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var after = (await scope.ServiceProvider.GetRequiredService<DocumentsService>().QueryAsync(null, null, CancellationToken.None)).Single(d => d.Id == id);
            Assert.StartsWith("/docs/", path, StringComparison.Ordinal);
            Assert.Equal(before + 1, after.Downloads);
        }
    }

    [Fact]
    public async Task Guide_by_slug_loads_ordered_chapters_and_unpublished_guides_are_listed_but_not_served()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<DocumentsService>();

        var guide = await service.GuideBySlugAsync("beginner-12-weeks", CancellationToken.None);
        var guides = await service.GuidesAsync(CancellationToken.None);

        Assert.NotNull(guide);
        Assert.Equal(["Swim", "Bike", "Run", "The 12-week plan"], guide.Chapters.Select(c => c.TitleEn));
        Assert.Equal(4, guides.Count);
        Assert.Null(await service.GuideBySlugAsync("olympic-progression", CancellationToken.None));
    }
}
