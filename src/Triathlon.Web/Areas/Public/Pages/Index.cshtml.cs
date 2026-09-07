using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// The public home page. The three cards under "Upcoming events" are the next three start lines in
/// the database, rendered by the server — the client-side <c>data.js</c> they used to come from is
/// gone, which is what lets the page keep a <c>script-src 'self'</c> policy with no inline script.
/// <para>
/// Cached under the <c>Home</c> and <c>Events</c> tags, so publishing either the page's own content
/// or an event drops it immediately rather than leaving the site a minute behind the editor.
/// </para>
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Home, CacheTags.Events])]
public sealed class IndexModel(IStringLocalizer<Shared> localizer, EventsService events) : PageModel
{
    public IReadOnlyList<Event> Upcoming { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Upcoming = await events.UpcomingAsync(null, null, 3, ct);

        ViewData["Title"] = localizer["HomeTitle"].Value;
        ViewData["today"] = events.Today;
    }
}
