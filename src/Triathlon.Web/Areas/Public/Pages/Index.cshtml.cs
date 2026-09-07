using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Domain.Stats;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// The public home page. The three cards under "Upcoming events" are the next three start lines in
/// the database, rendered by the server — the client-side <c>data.js</c> they used to come from is
/// gone, which is what lets the page keep a <c>script-src 'self'</c> policy with no inline script.
/// The stat band is the four KPIs flagged <c>ShowOnHome</c>, in their <c>HomeOrder</c>.
/// <para>
/// Cached under the <c>Home</c>, <c>Events</c>, <c>Stats</c> and <c>News</c> tags, so publishing
/// the page's own content, an event, a KPI or an article drops it immediately rather than leaving
/// the site a minute behind the editor.
/// </para>
/// </summary>
[OutputCache(
    PolicyName = OutputCacheSetup.PublicPolicy,
    Tags = [CacheTags.Home, CacheTags.Events, CacheTags.Stats, CacheTags.News])]
public sealed class IndexModel(
    IStringLocalizer<Shared> localizer, EventsService events, StatsService stats, NewsService news, ContentService content)
    : PageModel
{
    /// <summary>How many articles the home page's news band shows.</summary>
    private const int LatestNews = 3;

    public IReadOnlyList<Event> Upcoming { get; private set; } = [];

    public IReadOnlyList<Kpi> HomeKpis { get; private set; } = [];

    public IReadOnlyList<NewsPost> Latest { get; private set; } = [];

    /// <summary>
    /// The <c>home</c> CMS page's blocks: the hero copy first, then the season teaser, the quick-path
    /// cards and the final call-to-action. Empty on an unseeded database — the page still renders,
    /// just without those sections, rather than throwing.
    /// </summary>
    public IReadOnlyList<PageBlock> Blocks { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Upcoming = await events.UpcomingAsync(null, null, 3, ct);
        HomeKpis = await stats.HomeKpisAsync(ct);
        Latest = await news.LatestAsync(LatestNews, ct);
        Blocks = (await content.PageAsync("home", ct))?.Blocks ?? [];

        ViewData["Title"] = localizer["HomeTitle"].Value;
        ViewData["today"] = events.Today;

        // Built from a slug, so it cannot sit in the [OutputCache] attribute above (which needs a
        // compile-time constant) — added here instead, exactly as the other CMS-backed pages do.
        HttpContext.Features.Get<IOutputCacheFeature>()?.Context.Tags.Add(CacheTags.Page("home"));
    }
}
