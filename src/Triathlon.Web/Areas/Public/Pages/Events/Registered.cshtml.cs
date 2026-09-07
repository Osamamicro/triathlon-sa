using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Events;

/// <summary>
/// Where a guest lands after the entry endpoint has written their registration — the "get" of
/// post/redirect/get, so a refresh cannot enter them twice. Uncached: it is one visitor's own
/// confirmation, addressed to them by name.
/// </summary>
public sealed class RegisteredModel(EventsService events) : PageModel
{
    public Event Event { get; private set; } = null!;

    public bool Waitlisted { get; private set; }

    /// <summary>The name the visitor typed, carried on the redirect and re-encoded by Razor.</summary>
    public string? Name { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, string? outcome, string? name, CancellationToken ct)
    {
        var found = await events.BySlugAsync(slug, ct);
        if (found is null)
        {
            return NotFound();
        }

        Event = found;
        Waitlisted = outcome == "waitlist";
        Name = name;

        ViewData["Title"] = PublicText.Bi("Entry received", "تم استلام التسجيل");

        return Page();
    }
}
