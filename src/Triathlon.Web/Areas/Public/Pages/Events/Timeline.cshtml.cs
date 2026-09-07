using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Events;

/// <summary>
/// One season as a journey across the Kingdom: the calendar down the left, the map beside it.
/// <para>
/// Everything is rendered here — every item, every month heading, every marker — and
/// <c>timeline.js</c> only toggles classes on what the server already sent. That keeps the page
/// readable without script, indexable, and cacheable: the only query key it varies on is the
/// season, because the type filter is a client-side view of markup that is already present.
/// </para>
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Events], VaryByQueryKeys = ["season"])]
public sealed class TimelineModel(EventsService events) : PageModel
{
    public TimelineData Data { get; private set; } = null!;

    public DateOnly Today { get; private set; }

    public async Task OnGetAsync(string? season, CancellationToken ct)
    {
        Data = await events.TimelineAsync(season, null, ct);

        // A season nobody races in — a stale link, a typo — shows the current season rather than an
        // empty page, the same reading the events list gives an unknown city.
        if (season is not null && !Data.Seasons.Contains(season, StringComparer.Ordinal))
        {
            Data = await events.TimelineAsync(null, null, ct);
        }

        Today = events.Today;

        ViewData["Title"] = PublicText.Bi("The Season, Mapped", "الموسم على الخريطة");
        ViewData["today"] = Today;
    }
}
