using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// The CMS half of the public site: a page is an ordered list of blocks, the navigation and the
/// club and committee lists are rows rather than markup, and the news list is filtered by the
/// Riyadh date so a post scheduled for next week stays invisible until then.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class ContentServiceTests(WebAppFixture app)
{
    [Fact]
    public async Task Join_page_has_its_blocks_in_order()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var page = await scope.ServiceProvider.GetRequiredService<ContentService>().PageAsync("join", CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal([BlockType.Hero, BlockType.Steps, BlockType.Table, BlockType.Cards, BlockType.Clubs, BlockType.Cta], page.Blocks.Select(b => b.Type));
        Assert.Equal(4, page.Blocks[1].Items.Count);
        Assert.Equal("clubs", page.Blocks[4].Anchor);
    }

    [Fact]
    public void Block_items_round_trip_through_json()
    {
        var items = new[] { new BlockItem(TitleEn: "A", TitleAr: "أ", CellsEn: ["1", "2"], CellsAr: ["١", "٢"]) };
        var json = BlockItem.Serialize(items);
        var back = BlockItem.Parse(json);
        var item = back[0];
        Assert.Equal("أ", item.TitleAr);
        Assert.NotNull(item.CellsAr);
        Assert.Equal(["١", "٢"], item.CellsAr);
        Assert.DoesNotContain("null", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Navigation_and_clubs_and_committees_are_seeded()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var content = scope.ServiceProvider.GetRequiredService<ContentService>();

        Assert.Equal(8, (await content.NavigationAsync(NavLocation.Header, CancellationToken.None)).Count);
        Assert.Equal(6, (await content.ClubsAsync(CancellationToken.None)).Count);
        Assert.Equal(4, (await content.CommitteesAsync(CancellationToken.None)).Count);
    }

    [Fact]
    public async Task News_lists_published_posts_newest_first_and_hides_future_ones()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var future = new NewsPost
        {
            Slug = "future-" + Guid.NewGuid().ToString("N")[..6],
            TitleEn = "F", TitleAr = "م", SummaryEn = "s", SummaryAr = "م",
            BodyEn = "<p>b</p>", BodyAr = "<p>ب</p>",
            PublishedOn = new DateOnly(2999, 1, 1), IsPublished = true,
        };
        db.NewsPosts.Add(future);
        await db.SaveChangesAsync();

        try
        {
            var latest = await scope.ServiceProvider.GetRequiredService<NewsService>().LatestAsync(10, CancellationToken.None);

            Assert.DoesNotContain(latest, p => p.Slug.StartsWith("future-", StringComparison.Ordinal));
            Assert.True(latest.Count >= 3);
            Assert.True(latest.Zip(latest.Skip(1)).All(pair => pair.First.PublishedOn >= pair.Second.PublishedOn));
        }
        finally
        {
            db.NewsPosts.Remove(future);
            await db.SaveChangesAsync();
        }
    }
}
