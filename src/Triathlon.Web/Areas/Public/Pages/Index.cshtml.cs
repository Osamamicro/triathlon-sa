using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
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
/// Cached under the <c>Home</c>, <c>Events</c> and <c>Stats</c> tags, so publishing the page's own
/// content, an event or a KPI drops it immediately rather than leaving the site a minute behind
/// the editor.
/// </para>
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Home, CacheTags.Events, CacheTags.Stats])]
public sealed class IndexModel(IStringLocalizer<Shared> localizer, EventsService events, StatsService stats) : PageModel
{
    public IReadOnlyList<Event> Upcoming { get; private set; } = [];

    public IReadOnlyList<Kpi> HomeKpis { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Upcoming = await events.UpcomingAsync(null, null, 3, ct);
        HomeKpis = await stats.HomeKpisAsync(ct);

        ViewData["Title"] = localizer["HomeTitle"].Value;
        ViewData["today"] = events.Today;
    }
}
