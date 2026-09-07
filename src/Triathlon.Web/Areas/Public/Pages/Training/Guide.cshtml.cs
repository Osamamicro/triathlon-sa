using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Training;

/// <summary>
/// One training guide, chapter by chapter, with its PDF underneath. A guide that has not been
/// published has no page — it is a roadmap card on the section's front page, not an address — so
/// an unpublished slug is a 404 rather than a preview.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Guides])]
public sealed class GuideModel(DocumentsService documents) : PageModel
{
    public TrainingGuide Guide { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        var found = await documents.GuideBySlugAsync(slug, ct);
        if (found is null)
        {
            // A bare 404 here; the status-page branch in Program.cs turns it into the branded page.
            return NotFound();
        }

        Guide = found;

        ViewData["Title"] = PublicText.Bi(found.TitleEn, found.TitleAr);

        return Page();
    }
}
