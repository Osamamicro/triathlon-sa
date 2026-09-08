using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Data.Seed;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Data;

/// <summary>
/// <c>SeedStructure</c>'s per-slug/per-key guards used a filtered <c>AnyAsync</c>, so a row a
/// dashboard editor had soft-deleted was invisible to the guard even though the table's unique index
/// (on <c>Slug</c>, unfiltered) still saw it: the next boot's reseed tried to insert a second
/// <c>contact</c> page and crashed on the index instead of recognising the slug as already taken.
/// This exercises that path against the fixture's already-seeded database, rather than standing up a
/// brand-new one the way <see cref="SeedStructureTests"/> does for a from-scratch boot.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class SeedIdempotencyTests(WebAppFixture app)
{
    [Fact]
    public async Task Reseeding_after_a_page_is_soft_deleted_does_not_duplicate_it()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var content = scope.ServiceProvider.GetRequiredService<ContentService>();

        var contact = await db.Pages.AsNoTracking().SingleAsync(p => p.Slug == "contact");

        try
        {
            await content.DeletePageAsync(contact.Id, CancellationToken.None);
            Assert.Null(await db.Pages.SingleOrDefaultAsync(p => p.Id == contact.Id));

            // Must not throw — the pre-fix guard would try to re-insert "contact" here and crash on
            // the table's unfiltered unique index on Slug.
            await SeedStructure.RunAsync(scope.ServiceProvider, CancellationToken.None);

            var contactRows = await db.Pages.IgnoreQueryFilters().Where(p => p.Slug == "contact").ToListAsync();
            Assert.Single(contactRows);
            Assert.Equal(contact.Id, contactRows[0].Id);
            Assert.NotNull(contactRows[0].DeletedAt);
        }
        finally
        {
            await content.RestorePageAsync(contact.Id, CancellationToken.None);
        }
    }
}
