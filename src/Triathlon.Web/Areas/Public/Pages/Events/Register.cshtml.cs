using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Events;

/// <summary>
/// The guest entry form for one event. Deliberately NOT output-cached: the page carries an
/// antiforgery token, and a cached response would hand one visitor's token to everybody else.
/// </summary>
public sealed class RegisterModel(EventsService events) : PageModel
{
    public Event Event { get; private set; } = null!;

    /// <summary>Set by the API's redirect when the posted form did not validate.</summary>
    public bool Invalid { get; private set; }

    // The flag arrives as the API writes it, "?invalid=1", which the bool binder would read as
    // false — so it is bound as text and compared here.
    public async Task<IActionResult> OnGetAsync(string slug, string? invalid, CancellationToken ct)
    {
        var found = await events.BySlugAsync(slug, ct);
        if (found is null)
        {
            return NotFound();
        }

        // Entries closed, entries handled elsewhere, or the race is run: there is no form to show,
        // so the visitor goes to the event itself rather than to a dead end.
        if (found.RegistrationMode != RegistrationMode.Internal || found.StatusOn(events.Today) != EventStatus.Open)
        {
            return RedirectToPage("/Events/Detail", new { area = PublicSite.AreaName, culture = PublicText.Culture, slug });
        }

        Event = found;
        Invalid = invalid == "1";
        ViewData["Title"] = PublicText.Bi("Enter: ", "التسجيل في: ") + PublicText.Bi(found.TitleEn, found.TitleAr);

        return Page();
    }
}
