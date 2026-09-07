using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// The port of the prototype's <c>stats.html</c>: every KPI tile plus the two bar charts (athletes by
/// region, registered athletes by year), all server-rendered from the seeded statistics domain.
/// Cached under <see cref="CacheTags.Stats"/>, so a dashboard publish to any KPI, region or growth
/// point drops it immediately.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Stats])]
public sealed class StatisticsModel(StatsService stats) : PageModel
{
    public StatsOverview Overview { get; private set; } = new([], [], []);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Overview = await stats.AllAsync(ct);

        ViewData["Title"] = PublicText.Bi("Federation Statistics", "إحصائيات الاتحاد");
        ViewData["MetaDescription"] = PublicText.Bi(
            "Registered athletes, growth over time and participation by region. The Saudi Triathlon Federation's key numbers, updated as the season progresses.",
            "إحصاءات الاتحاد السعودي للترايثلون تشمل الرياضيين المسجلين، والنمو عبر الزمن، والمشاركة حسب المنطقة. تُحدَّث هذه الأرقام باستمرار مع تقدم الموسم.");
    }
}
