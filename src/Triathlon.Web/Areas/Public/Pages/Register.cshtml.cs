using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Triathlon.Web.Domain.Crm;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// The federation's athlete registration form — one application per season, reviewed by the
/// membership desk. Deliberately NOT output-cached: the page carries an antiforgery token, and a
/// cached response would hand one visitor's token to everybody else.
/// </summary>
public sealed class RegisterModel(
    EventsService events,
    ContentService content,
    IOptions<TurnstileOptions> turnstileOptions) : PageModel
{
    public IReadOnlyList<City> Cities { get; private set; } = [];

    public IReadOnlyList<Club> Clubs { get; private set; } = [];

    /// <summary>Everything still to be raced, so an applicant can name the event that brought them.</summary>
    public IReadOnlyList<Event> UpcomingEvents { get; private set; } = [];

    /// <summary>The id of the event named by <c>?event=slug</c>, as the select's option value.</summary>
    public string PrefilledEventId { get; private set; } = "";

    /// <summary>The categories the federation registers, in the order the form offers them.</summary>
    public static IReadOnlyList<string> Categories => AthleteCategories.All;

    /// <summary>
    /// The bounds the date picker offers, which are the same ones the endpoint enforces — the page
    /// asks <see cref="CrmService"/> for them rather than repeating the rule and drifting from it.
    /// </summary>
    public string EarliestDateOfBirth { get; private set; } = "";

    public string LatestDateOfBirth { get; private set; } = "";

    /// <summary>Set by the API's redirect when the posted form did not validate.</summary>
    public bool Invalid { get; private set; }

    /// <summary>Set by the API's redirect when the challenge, not the form, was the problem.</summary>
    public bool ChallengeFailed { get; private set; }

    /// <summary>The public half of the Turnstile key pair, or null when the challenge is off.</summary>
    public string? TurnstileSiteKey => turnstileOptions.Value.Enabled ? turnstileOptions.Value.SiteKey : null;

    /// <summary>What the visitor posted, keyed by lower-camel-case field name — empty unless <see cref="Invalid"/>.</summary>
    public IReadOnlyDictionary<string, string> Posted { get; private set; } = new Dictionary<string, string>();

    /// <summary>Field names that failed validation on the last post — empty unless <see cref="Invalid"/>.</summary>
    public IReadOnlySet<string> InvalidFields { get; private set; } = new HashSet<string>();

    // The flags arrive as the API writes them, "?invalid=1" and "?turnstile=1", which the bool
    // binder would read as false — so they are bound as text and compared here. The event is named
    // by slug, the way an event page links here; the select itself carries ids.
    public async Task OnGetAsync(string? @event, string? invalid, string? turnstile, CancellationToken ct)
    {
        Cities = await events.CitiesAsync(ct);
        Clubs = await content.ClubsAsync(ct);
        UpcomingEvents = await events.UpcomingAsync(null, null, 0, ct);

        if (!string.IsNullOrWhiteSpace(@event))
        {
            PrefilledEventId = UpcomingEvents.FirstOrDefault(e => e.Slug == @event)?.Id.ToString() ?? "";
        }

        var (earliest, latest) = CrmService.DateOfBirthRange(events.Today);
        EarliestDateOfBirth = earliest.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        LatestDateOfBirth = latest.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        Invalid = invalid == "1";
        ChallengeFailed = turnstile == "1";

        if (Invalid)
        {
            // TempData is one-time-read: this both fetches and clears the round trip the API's
            // redirect stashed, so refreshing this page a second time shows a normal blank form.
            (Posted, InvalidFields) = FormRoundTrip.TryRead(TempData);
        }

        ViewData["Title"] = PublicText.Bi("Athlete registration", "تسجيل الرياضيين");
    }

    /// <summary>
    /// The value the visitor typed for <paramref name="field"/> on the rejected post, or
    /// <paramref name="fallback"/> — which is how a select keeps its default on a first visit and
    /// the visitor's own choice on a second.
    /// </summary>
    public string Value(string field, string fallback = "") =>
        Posted.TryGetValue(field, out var value) && value.Length > 0 ? value : fallback;

    /// <summary>The <c>.field</c> wrapper's class, flagged when <paramref name="field"/> failed validation.</summary>
    public string FieldClass(string field) => InvalidFields.Contains(field) ? "field invalid" : "field";
}
