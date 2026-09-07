using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// The header's links are rows, not markup: an editor renames one, evicts the site tag, and the
/// next request shows the new label. That is the whole contract behind
/// <see cref="CacheTags.Site"/>, and this is the test that keeps it honest.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class NavigationTests(WebAppFixture app)
{
    [Fact]
    public async Task Header_navigation_comes_from_the_database_and_evicts_with_the_site_tag()
    {
        using var client = app.CreateClient();
        var home = await client.GetStringAsync("/en");
        Assert.Contains(">Statistics</a>", home, StringComparison.Ordinal);

        // A stored href is a path inside the culture, so it has to come out resolved. This also
        // pins the call to PublicText.Href: RazorPageBase.Href would emit the raw "statistics".
        Assert.Contains("href=\"/en/statistics\"", home, StringComparison.Ordinal);

        Guid id; string original;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var item = await db.NavItems.SingleAsync(n => n.Location == NavLocation.Header && n.Href == "statistics");
            id = item.Id; original = item.LabelEn;
            item.LabelEn = "Numbers";
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IOutputCacheStore>().EvictAsync(CancellationToken.None, CacheTags.Site);
        }

        try
        {
            Assert.Contains(">Numbers</a>", await client.GetStringAsync("/en"), StringComparison.Ordinal);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.NavItems.SingleAsync(n => n.Id == id)).LabelEn = original;
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IOutputCacheStore>().EvictAsync(CancellationToken.None, CacheTags.Site);
        }
    }
}
