using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.News;

/// <summary>
/// One news article. A post dated ahead of today in Riyadh is scheduled rather than published, so
/// its slug answers 404 until that morning arrives.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.News])]
public sealed class PostModel(NewsService news) : PageModel
{
    public NewsPost Post { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        var found = await news.BySlugAsync(slug, ct);
        if (found is null)
        {
            // A bare 404 here; the status-page branch in Program.cs turns it into the branded page.
            return NotFound();
        }

        Post = found;
        ViewData["Title"] = PublicText.Bi(found.TitleEn, found.TitleAr);

        return Page();
    }
}
