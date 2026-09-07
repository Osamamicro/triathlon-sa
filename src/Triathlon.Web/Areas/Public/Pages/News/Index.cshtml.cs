using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.News;

/// <summary>The newsroom: every live post, newest first.</summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.News])]
public sealed class IndexModel(NewsService news) : PageModel
{
    public IReadOnlyList<NewsPost> Posts { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Posts = await news.AllAsync(ct);

        ViewData["Title"] = PublicText.Bi("News", "الأخبار");
        ViewData["MetaDescription"] = PublicText.Bi(
            "The latest news from the Saudi Triathlon Federation: event recaps, calendar announcements and federation updates.",
            "أحدث أخبار الاتحاد السعودي للترايثلون: ملخصات الفعاليات، إعلانات التقويم، ومستجدات الاتحاد.");
    }
}
