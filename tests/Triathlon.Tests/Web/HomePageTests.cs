using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// The home page's copy comes from the <c>home</c> CMS page's blocks now, not from static markup in
/// <c>Index.cshtml</c> — this proves an edit to the hero block reaches the rendered page, while the
/// hero course diagram, the stat band and the CTA banners (design elements, not editable copy) stay.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class HomePageTests(WebAppFixture app)
{
    [Fact]
    public async Task Home_hero_and_sections_come_from_the_home_page_blocks()
    {
        Guid blockId; string original;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hero = await db.PageBlocks.Where(b => b.Type == BlockType.Hero).Join(db.Pages.Where(p => p.Slug == "home"), b => b.PageId, p => p.Id, (b, _) => b).SingleAsync();
            blockId = hero.Id; original = hero.TitleAr!;
            hero.TitleAr = "عنوان تجريبي";
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IOutputCacheStore>().EvictAsync(CancellationToken.None, CacheTags.Home);
        }
        try
        {
            using var client = app.CreateClient();
            var html = await client.GetStringAsync("/ar");
            Assert.Contains("عنوان تجريبي", html, StringComparison.Ordinal);
            Assert.Contains("class=\"tri-strip", html, StringComparison.Ordinal);      // design elements stay
            Assert.Contains("class=\"stat-band\"", html, StringComparison.Ordinal);
            Assert.Contains("class=\"cta-banner", html, StringComparison.Ordinal);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.PageBlocks.SingleAsync(b => b.Id == blockId)).TitleAr = original;
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IOutputCacheStore>().EvictAsync(CancellationToken.None, CacheTags.Home);
        }
    }
}
