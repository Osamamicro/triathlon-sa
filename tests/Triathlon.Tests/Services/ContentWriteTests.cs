using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// The write half of the CMS: a page save is one activity-log row and one eviction of its own tag,
/// navigation and settings evict the whole site, and delete/restore follow ADR 0001.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class ContentWriteTests(WebAppFixture app)
{
    private static PageInput NewPage(string slug, string bodyEn = "<p>Hello</p>") => new(
        slug, "Test page", "صفحة اختبار", null, null, true,
        [new BlockInput(null, 1, BlockType.RichText, null, null, null, null, "Title", "عنوان", bodyEn, "<p>مرحبا</p>", [], null, null, null, null, null, null)]);

    [Fact]
    public async Task Saving_a_page_sanitises_its_bodies_logs_a_diff_and_evicts_its_own_tag()
    {
        var slug = "write-" + Guid.NewGuid().ToString("N")[..8];
        using var client = app.CreateClient();

        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var content = scope.ServiceProvider.GetRequiredService<ContentService>();
            id = (await content.CreatePageAsync(NewPage(slug, "<p>Hello<script>alert(1)</script></p>"), CancellationToken.None)).Id;
        }

        try
        {
            var first = await client.GetStringAsync("/en/" + slug);
            Assert.Contains("<p>Hello</p>", first, StringComparison.Ordinal);
            // Not a blanket "no <script> anywhere on the page" check: the layout itself carries the
            // site's own theme/site scripts on every response. What must be gone is the editor's payload.
            Assert.DoesNotContain("<script>alert", first, StringComparison.Ordinal);
            Assert.True((await client.GetAsync("/en/" + slug)).Headers.Contains("Age"));

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var content = scope.ServiceProvider.GetRequiredService<ContentService>();
                var input = NewPage(slug, "<p>Changed</p>") with { TitleEn = "Renamed" };
                await content.UpdatePageAsync(id, input, CancellationToken.None);
            }

            using var after = await client.GetAsync("/en/" + slug);
            Assert.False(after.Headers.Contains("Age"), "page:{slug} was not evicted by the save");
            Assert.Contains("<p>Changed</p>", await after.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var log = await db.ActivityLogs.Where(l => l.Entity == "Page" && l.EntityId == id.ToString()).OrderBy(l => l.At).ToListAsync();
                Assert.Equal(["create", "update"], log.Select(l => l.Action));
                Assert.Contains("\"titleEn\":\"Test page\"", log[1].Diff, StringComparison.Ordinal);
                Assert.Contains("\"titleEn\":\"Renamed\"", log[1].Diff, StringComparison.Ordinal);
            }
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>().DeletePageAsync(id, CancellationToken.None);
        }
    }

    [Fact]
    public async Task A_reserved_or_malformed_slug_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var content = scope.ServiceProvider.GetRequiredService<ContentService>();

        var reserved = await Assert.ThrowsAsync<ContentValidationException>(() => content.CreatePageAsync(NewPage("events"), CancellationToken.None));
        Assert.Equal("Slug", reserved.Field);
        await Assert.ThrowsAsync<ContentValidationException>(() => content.CreatePageAsync(NewPage("Bad Slug"), CancellationToken.None));
        await Assert.ThrowsAsync<ContentValidationException>(() => content.CreatePageAsync(NewPage("join"), CancellationToken.None)); // already exists
    }

    [Fact]
    public async Task A_duplicate_block_id_is_refused_before_any_row_is_touched()
    {
        var slug = "dup-" + Guid.NewGuid().ToString("N")[..8];
        var otherSlug = "other-" + Guid.NewGuid().ToString("N")[..8];
        await using var scope = app.Services.CreateAsyncScope();
        var content = scope.ServiceProvider.GetRequiredService<ContentService>();

        var page = await content.CreatePageAsync(NewPage(slug), CancellationToken.None);
        var otherPage = await content.CreatePageAsync(NewPage(otherSlug), CancellationToken.None);
        try
        {
            var existingBlockId = page.Blocks.Single().Id;

            var duplicateBlocks = new BlockInput[]
            {
                new(existingBlockId, 1, BlockType.RichText, null, null, null, null, "One", "واحد", "<p>One</p>", "<p>واحد</p>", [], null, null, null, null, null, null),
                new(existingBlockId, 2, BlockType.RichText, null, null, null, null, "Two", "اثنان", "<p>Two</p>", "<p>اثنان</p>", [], null, null, null, null, null, null),
            };
            var badInput = NewPage(slug) with { TitleEn = "Should not stick", Blocks = duplicateBlocks };

            var ex = await Assert.ThrowsAsync<ContentValidationException>(() => content.UpdatePageAsync(page.Id, badInput, CancellationToken.None));
            Assert.Equal("Blocks", ex.Field);

            // A following, unrelated save on a different page must not flush the first page's
            // half-applied assignment — the two-pass shape means nothing was ever assigned to it.
            await content.SetPagePublishedAsync(otherPage.Id, false, CancellationToken.None);

            await using var freshScope = app.Services.CreateAsyncScope();
            var db = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reread = await db.Pages.AsNoTracking().SingleAsync(p => p.Id == page.Id);
            Assert.Equal("Test page", reread.TitleEn);
        }
        finally
        {
            await content.DeletePageAsync(page.Id, CancellationToken.None);
            await content.DeletePageAsync(otherPage.Id, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Deleting_a_page_soft_deletes_its_blocks_and_restore_brings_them_back()
    {
        var slug = "trash-" + Guid.NewGuid().ToString("N")[..8];
        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var content = scope.ServiceProvider.GetRequiredService<ContentService>();
            id = (await content.CreatePageAsync(NewPage(slug), CancellationToken.None)).Id;
            await content.DeletePageAsync(id, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Null(await db.Pages.SingleOrDefaultAsync(p => p.Id == id));
            Assert.Empty(await db.PageBlocks.Where(b => b.PageId == id).ToListAsync());
            Assert.NotNull((await db.PageBlocks.IgnoreQueryFilters().SingleAsync(b => b.PageId == id)).DeletedAt);
            Assert.Single(await scope.ServiceProvider.GetRequiredService<ContentService>().PagesAsync(deletedOnly: true, CancellationToken.None), p => p.Id == id);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ContentService>().RestorePageAsync(id, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.NotNull(await db.Pages.SingleOrDefaultAsync(p => p.Id == id));
            Assert.Single(await db.PageBlocks.Where(b => b.PageId == id).ToListAsync());
            var actions = await db.ActivityLogs.Where(l => l.EntityId == id.ToString()).OrderBy(l => l.At).Select(l => l.Action).ToListAsync();
            Assert.Equal(["create", "delete", "restore"], actions);
        }
    }

    [Fact]
    public async Task A_slug_held_by_a_soft_deleted_page_is_reported_as_taken()
    {
        var slug = "gone-" + Guid.NewGuid().ToString("N")[..8];
        await using var scope = app.Services.CreateAsyncScope();
        var content = scope.ServiceProvider.GetRequiredService<ContentService>();

        var id = (await content.CreatePageAsync(NewPage(slug), CancellationToken.None)).Id;
        await content.DeletePageAsync(id, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() => content.CreatePageAsync(NewPage(slug), CancellationToken.None));
        Assert.Equal("Slug", ex.Field);
    }

    [Fact]
    public async Task Saving_the_navigation_reorders_and_evicts_the_whole_site()
    {
        using var client = app.CreateClient();
        _ = await client.GetStringAsync("/en/rules");
        Assert.True((await client.GetAsync("/en/rules")).Headers.Contains("Age"));

        IReadOnlyList<NavItem> original;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var content = scope.ServiceProvider.GetRequiredService<ContentService>();
            original = await content.NavigationForEditAsync(NavLocation.FooterCompete, CancellationToken.None);
            var reversed = original.Reverse().Select(n => new NavItemInput(n.Id, n.LabelEn, n.LabelAr, n.Href, n.IsPublished)).ToList();
            await content.SaveNavigationAsync(NavLocation.FooterCompete, reversed, CancellationToken.None);
        }

        try
        {
            using var response = await client.GetAsync("/en/rules");
            Assert.False(response.Headers.Contains("Age"), "site tag was not evicted by the navigation save");
            await using var scope = app.Services.CreateAsyncScope();
            var now = await scope.ServiceProvider.GetRequiredService<ContentService>().NavigationAsync(NavLocation.FooterCompete, CancellationToken.None);
            Assert.Equal(original.Reverse().Select(n => n.Id), now.Select(n => n.Id));
            Assert.Equal([1, 2, 3, 4], now.Select(n => n.SortOrder));
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            var content = scope.ServiceProvider.GetRequiredService<ContentService>();
            await content.SaveNavigationAsync(NavLocation.FooterCompete,
                original.Select(n => new NavItemInput(n.Id, n.LabelEn, n.LabelAr, n.Href, n.IsPublished)).ToList(), CancellationToken.None);
        }
    }

    [Fact]
    public async Task Settings_are_seeded_render_in_the_footer_and_a_save_evicts_the_site()
    {
        using var client = app.CreateClient();
        Assert.Contains("info@triathlon.sa", await client.GetStringAsync("/en/statistics"), StringComparison.Ordinal);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var content = scope.ServiceProvider.GetRequiredService<ContentService>();
            await content.SaveSettingsAsync([new SiteSettingInput("contact.email", "hello@triathlon.sa", "hello@triathlon.sa")], CancellationToken.None);
        }

        try
        {
            Assert.Contains("hello@triathlon.sa", await client.GetStringAsync("/en/statistics"), StringComparison.Ordinal);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ContentService>()
                .SaveSettingsAsync([new SiteSettingInput("contact.email", "info@triathlon.sa", "info@triathlon.sa")], CancellationToken.None);
        }
    }

    [Fact]
    public async Task An_unsafe_contact_website_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var content = scope.ServiceProvider.GetRequiredService<ContentService>();

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() => content.SaveSettingsAsync(
            [new SiteSettingInput(SettingKeys.ContactWebsite, "javascript:alert(1)", "javascript:alert(1)")], CancellationToken.None));
        Assert.Equal("contact.website", ex.Field);
    }

    [Fact]
    public async Task A_news_post_save_sanitises_and_evicts_news()
    {
        using var client = app.CreateClient();
        _ = await client.GetStringAsync("/en/news");
        Assert.True((await client.GetAsync("/en/news")).Headers.Contains("Age"));

        var slug = "news-" + Guid.NewGuid().ToString("N")[..8];
        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var news = scope.ServiceProvider.GetRequiredService<NewsService>();
            var post = await news.SaveAsync(new NewsPostInput(null, slug, "Write test", "اختبار", "s", "م",
                "<p>Body<img src=x onerror=alert(1)></p>", "<p>نص</p>", null, new DateOnly(2026, 9, 1), true), CancellationToken.None);
            id = post.Id;
            Assert.Equal("<p>Body</p>", post.BodyEn);
        }

        try
        {
            using var list = await client.GetAsync("/en/news");
            Assert.False(list.Headers.Contains("Age"));
            Assert.Contains("Write test", await list.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<NewsService>().DeleteAsync(id, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Saving_a_news_post_with_a_stale_id_is_refused_not_recreated()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var news = scope.ServiceProvider.GetRequiredService<NewsService>();
        var slug = "news-" + Guid.NewGuid().ToString("N")[..8];

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() => news.SaveAsync(
            new NewsPostInput(Guid.NewGuid(), slug, "Ghost", "شبح", "s", "م", "<p>Body</p>", "<p>نص</p>", null, new DateOnly(2026, 9, 1), true),
            CancellationToken.None));
        Assert.Equal("Id", ex.Field);
        Assert.Equal("Validation_NotFound", ex.Key);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await db.NewsPosts.SingleOrDefaultAsync(p => p.Slug == slug));
    }
}
