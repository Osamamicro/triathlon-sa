using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
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
public sealed class DetailModel(EventsService events, IStringLocalizer<Shared> localizer) : PageModel
{
    /// <summary>Saudi Arabia is UTC+3 year-round — the same offset the calendar itself keeps.</summary>
    private const string RiyadhOffset = "+03:00";

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
        ViewData["MetaDescription"] = PublicText.Bi(found.DescriptionEn, found.DescriptionAr);

        var origin = $"{Request.Scheme}://{Request.Host}";
        var canonicalUrl = origin + PublicCulture.PathForCulture(HttpContext, PublicText.Culture);

        var siteName = localizer["SiteName"].Value;

        if (found.HeroImagePath is { } hero)
        {
            var absoluteHero = hero.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || hero.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? hero
                : origin + hero;
            ViewData["OgImage"] = absoluteHero;
            ViewData["JsonLd"] = BuildJsonLd(found, origin, canonicalUrl, siteName, absoluteHero);
        }
        else
        {
            ViewData["JsonLd"] = BuildJsonLd(found, origin, canonicalUrl, siteName, image: null);
        }

        // The per-slug tag can only be added once the slug is known to be real, so it goes on the
        // cache entry here rather than in the attribute above.
        HttpContext.Features.Get<IOutputCacheFeature>()?.Context.Tags.Add(CacheTags.Event(found.Slug));

        return Page();
    }

    /// <summary>
    /// <c>SportsEvent</c> structured data for the page, serialised once as compact JSON (no
    /// indentation — the tests match the exact bytes) with <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>
    /// so the Arabic text renders literally instead of as a wall of <c>\uXXXX</c> escapes.
    /// <para>
    /// Relaxed escaping still leaves <c>&lt;</c> untouched, which is safe inside a JSON string but
    /// not inside an HTML <c>&lt;script&gt;</c> element — a title or description containing
    /// <c>&lt;/script&gt;</c> would otherwise close the tag early. <c>&lt;</c> is therefore replaced
    /// with its JSON escape <c><</c> by hand, after serialisation, which is valid JSON and
    /// cannot be reopened as markup.
    /// </para>
    /// </summary>
    private static string BuildJsonLd(Event ev, string origin, string canonicalUrl, string siteName, string? image)
    {
        var location = new JsonObject
        {
            ["@type"] = "Place",
            ["name"] = PublicText.Bi(ev.VenueEn, ev.VenueAr),
            ["address"] = new JsonObject
            {
                ["@type"] = "PostalAddress",
                ["addressLocality"] = PublicText.Bi(ev.City.NameEn, ev.City.NameAr),
                ["addressCountry"] = "SA",
            },
        };

        var organizer = new JsonObject
        {
            ["@type"] = "SportsOrganization",
            ["name"] = siteName,
            ["url"] = origin,
        };

        var jsonLd = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "SportsEvent",
            ["name"] = PublicText.Bi(ev.TitleEn, ev.TitleAr),
            ["description"] = PublicText.Bi(ev.DescriptionEn, ev.DescriptionAr),
            ["startDate"] = StartDate(ev),
            ["eventStatus"] = "https://schema.org/EventScheduled",
            ["location"] = location,
            ["organizer"] = organizer,
            ["url"] = canonicalUrl,
        };

        if (ev.DateEnd is { } end)
        {
            jsonLd["endDate"] = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (image is not null)
        {
            jsonLd["image"] = image;
        }

        var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        return jsonLd.ToJsonString(options).Replace("<", "\\u003c", StringComparison.Ordinal);
    }

    private static string StartDate(Event ev) => ev.StartTime is { } time
        ? $"{ev.DateStart:yyyy-MM-dd}T{time:HH:mm:ss}{RiyadhOffset}"
        : ev.DateStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
