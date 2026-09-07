using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Training;

/// <summary>
/// The training section's front page: the guides that are readable today, the ones being written,
/// and whatever the editors have put on the <c>training</c> page beneath them.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Guides])]
public sealed class IndexModel(DocumentsService documents, ContentService content) : PageModel
{
    public IReadOnlyList<TrainingGuide> Published { get; private set; } = [];

    /// <summary>Guides still being written; they render as the roadmap's "in preparation" cards.</summary>
    public IReadOnlyList<TrainingGuide> InPreparation { get; private set; } = [];

    /// <summary>The blocks of the <c>training</c> CMS page, rendered under the guides.</summary>
    public IReadOnlyList<PageBlock> Trailing { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var guides = await documents.GuidesAsync(ct);
        Published = guides.Where(g => g.IsPublished).ToList();
        InPreparation = guides.Where(g => !g.IsPublished).ToList();
        Trailing = (await content.PageAsync("training", ct))?.Blocks ?? [];

        ViewData["Title"] = PublicText.Bi("Training Guide", "دليل التدريب");

        // The page's own tag is built from a slug, so it goes on the cache entry here: an
        // [OutputCache] tag has to be a compile-time constant.
        HttpContext.Features.Get<IOutputCacheFeature>()?.Context.Tags.Add(CacheTags.Page("training"));
    }
}
