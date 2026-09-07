using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Events;

/// <summary>
/// One event: what it is, where and when, how to enter, and — once it has been raced — its results.
/// <para>
/// Cached under both the section tag and a tag of its own, so editing one event drops that event's
/// two pages instead of the whole calendar.
/// </para>
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Events])]
public sealed class DetailModel(EventsService events) : PageModel
{
    public Event Event { get; private set; } = null!;

    public EventStatus Status { get; private set; }

    public DateOnly Today { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        var found = await events.BySlugAsync(slug, ct);
        if (found is null)
        {
            // A bare 404 here; the status-page branch in Program.cs turns it into the branded page.
            return NotFound();
        }

        Event = found;
        Today = events.Today;
        Status = found.StatusOn(Today);
        ViewData["Title"] = PublicText.Bi(found.TitleEn, found.TitleAr);

        // The per-slug tag can only be added once the slug is known to be real, so it goes on the
        // cache entry here rather than in the attribute above.
        HttpContext.Features.Get<IOutputCacheFeature>()?.Context.Tags.Add(CacheTags.Event(found.Slug));

        return Page();
    }
}
