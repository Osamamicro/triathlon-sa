using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Triathlon.Web.Areas.Public;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Api;

/// <summary>
/// The handful of endpoints the public site needs beyond its pages. POSTs live here rather than
/// on the pages because rate limiting is per endpoint and a Razor Page is one endpoint for GET and
/// POST alike. Form binding on a minimal API validates the antiforgery token automatically.
/// </summary>
public static class PublicApi
{
    /// <summary>The guest entry form as the browser posts it; property names match the field names.</summary>
    public sealed class GuestEntryForm
    {
        [Required, RegularExpression("^(en|ar)$")] public string Culture { get; set; } = "en";

        [Required, StringLength(128, MinimumLength = 2)] public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";

        [Phone, StringLength(32)] public string? Phone { get; set; }

        [Required, StringLength(64)] public string Category { get; set; } = "";

        [StringLength(128)] public string? Club { get; set; }

        /// <summary>The medical/rules declaration checkbox: absent when unticked, so Required covers it.</summary>
        [Required] public string Declaration { get; set; } = "";
    }

    public static IEndpointRouteBuilder MapPublicApi(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var api = app.MapGroup("/api");

        // The same season the timeline page renders, as data: the Kingdom map's coordinates and one
        // entry per event, in whichever culture the caller asks for. Cached under the events tag, so
        // publishing an event drops the page and the feed together.
        api.MapGet("/timeline", async (string? season, string? type, string? culture, EventsService events, CancellationToken ct) =>
        {
            var ar = string.Equals(culture, "ar", StringComparison.OrdinalIgnoreCase);
            var lang = ar ? "ar" : PublicSite.DefaultCulture;
            var data = await events.TimelineAsync(season, ParseType(type), ct);
            var today = events.Today;

            return Results.Ok(new
            {
                data.Season,
                data.Seasons,
                Cities = data.Cities.Select(c => new { c.Key, Name = ar ? c.NameAr : c.NameEn, X = c.SvgX, Y = c.SvgY, c.LabelAtEnd, c.LabelDy }),
                Events = data.Events.Select(e => new
                {
                    e.Slug,
                    Url = PublicCulture.Url(lang, "events/" + e.Slug),
                    Title = ar ? e.TitleAr : e.TitleEn,
                    Type = e.Type.ToString().ToLowerInvariant(),
                    Status = e.StatusOn(today).ToString(),
                    Date = e.DateStart,
                    Time = e.StartTime,
                    CityKey = e.City.Key,
                    Venue = ar ? e.VenueAr : e.VenueEn,
                    Swim = e.SwimDistance,
                    Bike = e.BikeDistance,
                    Run = e.RunDistance,
                }),
            });
        })
        .CacheOutput(policy => policy
            .Expire(OutputCacheSetup.PublicLifetime)
            .Tag(CacheTags.Events, CacheTags.Site)
            .SetVaryByQuery("season", "type", "culture"));

        // The whole calendar as a subscribable feed, so an athlete's phone keeps the season without
        // visiting the site again.
        api.MapGet("/calendar.ics", async (string? type, string? culture, EventsService events, HttpContext http, CancellationToken ct) =>
        {
            var lang = string.Equals(culture, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : PublicSite.DefaultCulture;
            var list = await events.AllPublishedAsync(ParseType(type), ct);
            var ics = IcsWriter.Write(list, lang, $"{http.Request.Scheme}://{http.Request.Host}");

            return Results.Text(ics, "text/calendar", Encoding.UTF8);
        })
        .CacheOutput(policy => policy
            .Expire(OutputCacheSetup.PublicLifetime)
            .Tag(CacheTags.Events, CacheTags.Site)
            .SetVaryByQuery("type", "culture"));

        api.MapPost("/events/{slug}/register", async (
            string slug,
            [FromForm] GuestEntryForm form,
            EventsService events,
            ITempDataDictionaryFactory tempDataFactory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            // Every redirect below is built from a culture this method chose, never from the posted
            // one: an unvalidated value would land in a Location header and make this an open
            // redirect on the very branch that exists to handle bad input.
            var culture = form.Culture == "ar" ? "ar" : PublicSite.DefaultCulture;
            var path = $"events/{Uri.EscapeDataString(slug)}";

            // A browser posts "" for an optional field the visitor left alone, and [Phone] rejects
            // an empty string as readily as a malformed one — so blanks become "absent" first.
            form.Phone = NullIfBlank(form.Phone);
            form.Club = NullIfBlank(form.Club);

            // Loaded before validation so a posted category can be checked against the event's own
            // list — a crafted POST must not be able to write an arbitrary 64-char category string.
            var ev = await events.BySlugAsync(slug, ct);

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(form, new ValidationContext(form), results, validateAllProperties: true);

            if (ev is not null && ev.CategoryList.Count > 0 && !ev.CategoryList.Contains(form.Category, StringComparer.Ordinal))
            {
                isValid = false;
                results.Add(new ValidationResult("Category is not offered for this event.", [nameof(GuestEntryForm.Category)]));
            }

            if (!isValid)
            {
                // The event might not exist at all (a stale or crafted slug); there is no form to
                // hand a round trip back to, so this falls through to the same redirect and the
                // page itself renders the branded 404.
                if (ev is not null)
                {
                    var invalidFields = results
                        .SelectMany(r => r.MemberNames)
                        .Select(ToFieldName)
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    // Empty string, not null, for the optional fields: TryRead hands this back out as
                    // a non-nullable IReadOnlyDictionary<string, string>, and an absent value there
                    // reads the same as "not typed" either way.
                    var posted = new Dictionary<string, string?>
                    {
                        [nameof(GuestEntryForm.FullName)] = form.FullName,
                        [nameof(GuestEntryForm.Email)] = form.Email,
                        [nameof(GuestEntryForm.Phone)] = form.Phone ?? "",
                        [nameof(GuestEntryForm.Category)] = form.Category,
                        [nameof(GuestEntryForm.Club)] = form.Club ?? "",
                        [nameof(GuestEntryForm.Declaration)] = form.Declaration,
                    }.ToDictionary(kv => ToFieldName(kv.Key), kv => kv.Value);

                    // A minimal API delegate never runs through MVC's result filters, which is what
                    // saves TempData for a Razor Page automatically — so this endpoint saves it by
                    // hand. Save() writes the cookie onto HttpContext.Response synchronously, which
                    // happens here, before the IResult below sets the redirect's status line and
                    // Location header, so both land on the same 302 response.
                    var tempData = tempDataFactory.GetTempData(httpContext);
                    FormRoundTrip.Store(tempData, posted, invalidFields);
                    tempData.Save();
                }

                return Results.Redirect(PublicCulture.Url(culture, path + "/register") + "?invalid=1");
            }

            var outcome = await events.RegisterAsync(
                slug, new GuestRegistration(form.FullName, form.Email, form.Phone, form.Category, form.Club), ct);

            var confirmation = PublicCulture.Url(culture, path + "/registered");
            var name = "&name=" + Uri.EscapeDataString(form.FullName);

            return outcome switch
            {
                RegistrationOutcome.Confirmed => Results.Redirect(confirmation + "?outcome=confirmed" + name),
                RegistrationOutcome.Waitlist => Results.Redirect(confirmation + "?outcome=waitlist" + name),
                RegistrationOutcome.Closed => Results.Redirect(PublicCulture.Url(culture, path)),
                _ => Results.NotFound(),
            };
        })
        .RequireRateLimiting(RateLimitSetup.PublicPost)
        // .NET 10 minimal-API validation would answer 400 JSON to a browser form; we validate the
        // same attributes by hand above and redirect back to the form instead.
        .DisableValidation();

        return app;
    }

    /// <summary>An unknown value is no filter at all, the same reading the events list gives it.</summary>
    private static EventType? ParseType(string? type) => type?.ToLowerInvariant() switch
    {
        "competition" => EventType.Competition,
        "community" => EventType.Community,
        _ => null,
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// <see cref="ValidationResult.MemberNames"/> carries the C# property name ("FullName"); the
    /// form field, the TempData round trip and the "field invalid" CSS hook all key off the
    /// lower-camel-case name the HTML input uses instead ("fullName").
    /// </summary>
    private static string ToFieldName(string memberName) =>
        memberName.Length == 0 ? memberName : char.ToLowerInvariant(memberName[0]) + memberName[1..];
}
