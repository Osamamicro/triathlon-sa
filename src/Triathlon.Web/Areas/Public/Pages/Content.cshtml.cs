using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// Every CMS page that is not a section of its own — join and contact today, whatever the editors
/// add tomorrow — rendered as its ordered list of blocks.
/// <para>
/// Its route is a single parameter segment, which routing ranks below every literal one, so a
/// dedicated page such as <c>/rules</c> keeps its own address and only an unclaimed slug reaches
/// here. An unknown or unpublished slug is a 404, not an empty page.
/// </para>
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy)]
public sealed class ContentModel(ContentService content) : PageModel
{
    /// <summary>
    /// The page being rendered. Fully qualified: <c>Page</c> alone is ambiguous between the CMS
    /// entity and <see cref="Microsoft.AspNetCore.Mvc.RazorPages.Page"/>.
    /// </summary>
    public Domain.Content.Page CmsPage { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        var found = await content.PageAsync(slug, ct);
        if (found is null)
        {
            // A bare 404 here; the status-page branch in Program.cs turns it into the branded page.
            return NotFound();
        }

        CmsPage = found;

        ViewData["Title"] = PublicText.Bi(found.TitleEn, found.TitleAr);

        var description = PublicText.IsArabic ? found.MetaDescriptionAr : found.MetaDescriptionEn;
        if (!string.IsNullOrWhiteSpace(description))
        {
            ViewData["MetaDescription"] = description;
        }

        // Neither tag can sit in the attribute: one is built from the slug the request asked for,
        // and the other depends on whether this page happens to embed the club list.
        var cache = HttpContext.Features.Get<IOutputCacheFeature>()?.Context;
        cache?.Tags.Add(CacheTags.Page(found.Slug));
        if (found.Blocks.Any(b => b.Type == BlockType.Clubs))
        {
            cache?.Tags.Add(CacheTags.Clubs);
        }

        return Page();
    }
}
