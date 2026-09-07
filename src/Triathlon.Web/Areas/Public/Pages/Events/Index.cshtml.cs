using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Events;

/// <summary>
/// Both calendars on one page: what is coming, and the archive of what has been raced. The two
/// filters are query-string state rather than script, so every combination is a shareable URL and
/// a cacheable response — hence <c>VaryByQueryKeys</c> on exactly the two keys the page reads.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Events], VaryByQueryKeys = ["type", "city"])]
public sealed class IndexModel(EventsService events) : PageModel
{
    public EventType? Type { get; private set; }

    public string? CityKey { get; private set; }

    public IReadOnlyList<City> Cities { get; private set; } = [];

    public IReadOnlyList<Event> Upcoming { get; private set; } = [];

    public IReadOnlyList<Event> Past { get; private set; } = [];

    public DateOnly Today { get; private set; }

    public async Task OnGetAsync(string? type, string? city, CancellationToken ct)
    {
        Type = type?.ToLowerInvariant() switch
        {
            "competition" => EventType.Competition,
            "community" => EventType.Community,
            _ => null,
        };

        Cities = await events.CitiesAsync(ct);

        // An unknown city is treated as no filter rather than as an empty page: a stale link should
        // show the calendar, not an apology.
        CityKey = Cities.Any(c => c.Key == city) ? city : null;

        Today = events.Today;
        Upcoming = await events.UpcomingAsync(Type, CityKey, 0, ct);
        Past = await events.PastAsync(Type, CityKey, ct);

        ViewData["Title"] = PublicText.Bi("Events & Calendar", "الفعاليات والتقويم");
        ViewData["MetaDescription"] = PublicText.Bi(
            "The national race calendar: every upcoming triathlon, duathlon and aquathlon in Saudi Arabia, filterable by discipline and city, plus the archive of past events.",
            "تقويم السباقات الوطني: جميع فعاليات الترايثلون والدواثلون والأكواثلون القادمة في المملكة، قابلة للتصفية حسب النوع والمدينة، بالإضافة إلى أرشيف الفعاليات السابقة.");
        ViewData["today"] = Today;
    }

    /// <summary>The URL of this page under a different filter, keeping the culture it is rendered in.</summary>
    public static string FilterUrl(string? type, string? city)
    {
        var query = new List<string>();
        if (type is not null)
        {
            query.Add("type=" + type);
        }

        if (city is not null)
        {
            query.Add("city=" + Uri.EscapeDataString(city));
        }

        return PublicCulture.Url(PublicText.Culture, "events") + (query.Count > 0 ? "?" + string.Join('&', query) : "");
    }

    /// <summary>The active type as it appears in the query string, so the city form can carry it.</summary>
    public string? TypeKey => Type switch
    {
        EventType.Competition => "competition",
        EventType.Community => "community",
        _ => null,
    };
}
