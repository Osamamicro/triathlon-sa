using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Net.Http.Headers;
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
    /// <summary>
    /// The TempData key both confirmation pages (<c>Registered.cshtml.cs</c>,
    /// <c>RegisterReceived.cshtml.cs</c>) read the visitor's name back from, instead of a
    /// <c>?name=</c> query string parameter — a query string is copyable and bookmarkable, and this
    /// is a one-time confirmation addressed to whoever just submitted the form, not a link anyone
    /// with the URL should be able to replay with their own name spliced in.
    /// </summary>
    public const string ConfirmationNameKey = "ConfirmationName";

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

    /// <summary>
    /// The athlete registration form as the browser posts it. Everything is text here, including the
    /// date and the two ids: what the visitor sends is a claim, and each one is checked against the
    /// database below rather than trusted to a binder.
    /// </summary>
    public sealed class AthleteRegistrationForm
    {
        [Required, RegularExpression("^(en|ar)$")] public string Culture { get; set; } = "en";

        [Required, StringLength(128, MinimumLength = 2)] public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";

        /// <summary>ISO yyyy-MM-dd, which is what <c>&lt;input type="date"&gt;</c> posts in every locale.</summary>
        [Required, StringLength(10)] public string DateOfBirth { get; set; } = "";

        [StringLength(64)] public string? City { get; set; }

        [Required, StringLength(64)] public string Category { get; set; } = "";

        /// <summary>A club id, or empty for "no club yet".</summary>
        [StringLength(64)] public string? Club { get; set; }

        /// <summary>The id of the event that brought them here, or empty.</summary>
        [StringLength(64)] public string? Event { get; set; }

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
        api.MapGet("/calendar.ics", async (string? type, string? culture, EventsService events, TimeProvider clock, HttpContext http, CancellationToken ct) =>
        {
            var lang = string.Equals(culture, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : PublicSite.DefaultCulture;
            var list = await events.AllPublishedAsync(ParseType(type), ct);
            var ics = IcsWriter.Write(list, lang, $"{http.Request.Scheme}://{http.Request.Host}", clock.GetUtcNow());

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
            form.Phone = FormPosts.NullIfBlank(form.Phone);
            form.Club = FormPosts.NullIfBlank(form.Club);

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
                    };

                    FormPosts.RoundTrip(tempDataFactory, httpContext, results, posted);
                }

                return Results.Redirect(PublicCulture.Url(culture, path + "/register") + "?invalid=1");
            }

            var outcome = await events.RegisterAsync(
                slug, new GuestRegistration(form.FullName, form.Email, form.Phone, form.Category, form.Club), ct);

            // The confirmation page reads this back out of TempData rather than a ?name= query
            // string — see ConfirmationNameKey.
            var tempData = tempDataFactory.GetTempData(httpContext);
            tempData[ConfirmationNameKey] = form.FullName;
            tempData.Save();

            var confirmation = PublicCulture.Url(culture, path + "/registered");

            return outcome switch
            {
                RegistrationOutcome.Confirmed => Results.Redirect(confirmation + "?outcome=confirmed"),
                RegistrationOutcome.Waitlist => Results.Redirect(confirmation + "?outcome=waitlist"),
                RegistrationOutcome.Closed => Results.Redirect(PublicCulture.Url(culture, path)),
                _ => Results.NotFound(),
            };
        })
        .RequireRateLimiting(RateLimitSetup.PublicPost)
        // .NET 10 minimal-API validation would answer 400 JSON to a browser form; we validate the
        // same attributes by hand above and redirect back to the form instead.
        .DisableValidation();

        // Athlete registration. Same shape as the guest entry above — validate by hand, round-trip a
        // rejection through TempData, redirect either way — with two additions: the posted ids are
        // resolved against the lists the form itself renders, and a Turnstile challenge stands in
        // front of the write when the Federation has configured one.
        api.MapPost("/register", async (
            [FromForm] AthleteRegistrationForm form,
            EventsService events,
            ContentService content,
            CrmService crm,
            ITurnstileVerifier turnstile,
            ITempDataDictionaryFactory tempDataFactory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            // Built from a culture this method chose, never from the posted one: an unvalidated
            // value would land in a Location header and make this an open redirect on the very
            // branch that exists to handle bad input.
            var culture = form.Culture == "ar" ? "ar" : PublicSite.DefaultCulture;
            var formUrl = PublicCulture.Url(culture, "register");

            // A browser posts "" for a select left on its blank option; those mean "not chosen".
            form.City = FormPosts.NullIfBlank(form.City);
            form.Club = FormPosts.NullIfBlank(form.Club);
            form.Event = FormPosts.NullIfBlank(form.Event);

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(form, new ValidationContext(form), results, validateAllProperties: true);

            void Reject(string reason, string field)
            {
                isValid = false;
                results.Add(new ValidationResult(reason, [field]));
            }

            // The five categories the form offers, and nothing else: a crafted POST must not be able
            // to write an arbitrary 64-character category onto an athlete record.
            if (!AthleteCategories.All.Contains(form.Category, StringComparer.Ordinal))
            {
                Reject("Category is not one the federation registers.", nameof(AthleteRegistrationForm.Category));
            }

            var (earliest, latest) = CrmService.DateOfBirthRange(events.Today);
            DateOnly? dateOfBirth = null;
            if (DateOnly.TryParseExact(form.DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var born)
                && born >= earliest && born <= latest)
            {
                dateOfBirth = born;
            }
            else
            {
                Reject("Date of birth is missing, malformed or outside the registrable ages.", nameof(AthleteRegistrationForm.DateOfBirth));
            }

            // The affiliated clubs the form listed. An id that is not among them is either a stale
            // option or a crafted post; neither should become a club affiliation on a CRM record.
            Guid? clubId = null;
            if (form.Club is not null)
            {
                var clubs = await content.ClubsAsync(ct);
                if (Guid.TryParse(form.Club, out var id) && clubs.Any(c => c.Id == id))
                {
                    clubId = id;
                }
                else
                {
                    Reject("Club is not on the federation's list.", nameof(AthleteRegistrationForm.Club));
                }
            }

            // Likewise the events the form listed: published, and still to be raced.
            Guid? interestEventId = null;
            if (form.Event is not null)
            {
                var upcoming = await events.UpcomingAsync(null, null, 0, ct);
                if (Guid.TryParse(form.Event, out var id) && upcoming.Any(e => e.Id == id))
                {
                    interestEventId = id;
                }
                else
                {
                    Reject("Event is not one of the upcoming published events.", nameof(AthleteRegistrationForm.Event));
                }
            }

            // The city is a convenience, not a claim worth rejecting an application over: an
            // unrecognised key is simply not recorded, and the desk asks when it matters.
            var cityKey = form.City is null
                ? null
                : (await events.CitiesAsync(ct)).FirstOrDefault(c => c.Key == form.City)?.Key;

            if (!isValid)
            {
                var posted = new Dictionary<string, string?>
                {
                    [nameof(AthleteRegistrationForm.FullName)] = form.FullName,
                    [nameof(AthleteRegistrationForm.Email)] = form.Email,
                    [nameof(AthleteRegistrationForm.DateOfBirth)] = form.DateOfBirth,
                    [nameof(AthleteRegistrationForm.City)] = form.City ?? "",
                    [nameof(AthleteRegistrationForm.Category)] = form.Category,
                    [nameof(AthleteRegistrationForm.Club)] = form.Club ?? "",
                    [nameof(AthleteRegistrationForm.Event)] = form.Event ?? "",
                    [nameof(AthleteRegistrationForm.Declaration)] = form.Declaration,
                };

                FormPosts.RoundTrip(tempDataFactory, httpContext, results, posted);

                return Results.Redirect(formUrl + "?invalid=1");
            }

            // After the form's own checks: there is nothing to protect until the input is worth
            // writing, and a visitor with a typo should see the typo rather than a challenge.
            // Cloudflare's field name is not a legal C# identifier, so it is read straight off the
            // form rather than bound. Verification is a no-op when no keys are configured.
            var challenge = httpContext.Request.Form["cf-turnstile-response"].ToString();
            if (!await turnstile.VerifyAsync(challenge, httpContext.Connection.RemoteIpAddress?.ToString(), ct))
            {
                return Results.Redirect(formUrl + "?turnstile=1");
            }

            // Created is ignored: a duplicate resolves to the athlete's existing pending application
            // rather than a new row, but the visitor sees the same "we received it" confirmation either way.
            var (_, _) = await crm.ApplyAsync(
                new AthleteApplication(form.FullName, form.Email, dateOfBirth!.Value, cityKey, form.Category, clubId, interestEventId, culture),
                ct);

            // The confirmation page reads this back out of TempData rather than a ?name= query
            // string — see ConfirmationNameKey.
            var tempData = tempDataFactory.GetTempData(httpContext);
            tempData[ConfirmationNameKey] = form.FullName;
            tempData.Save();

            return Results.Redirect(PublicCulture.Url(culture, "register/received"));
        })
        .RequireRateLimiting(RateLimitSetup.PublicPost)
        // .NET 10 minimal-API validation would answer 400 JSON to a browser form; we validate the
        // same attributes by hand above and redirect back to the form instead.
        .DisableValidation();

        MapDownload(app, "/documents/{id:guid}/download", DownloadKind.Document);
        MapDownload(app, "/rules/{id:guid}/download", DownloadKind.Rule);
        MapDownload(app, "/training/{id:guid}/download", DownloadKind.Guide);

        return app;
    }

    /// <summary>
    /// Records a download and redirects to the file, for the documents library, the rules page and
    /// the training guides alike — same shape, different table. Never cached: a stale 302 would point
    /// a returning visitor at a file that no longer exists, and the counter has to increment on every
    /// real download rather than once per cache window.
    /// </summary>
    private static void MapDownload(IEndpointRouteBuilder endpoints, string pattern, DownloadKind kind) =>
        endpoints.MapGet(pattern, async (Guid id, DocumentsService documents, ContentGuard guard, ILoggerFactory loggers, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers[HeaderNames.CacheControl] = "no-store";
            var path = await documents.RecordDownloadAsync(kind, id, ct);
            if (path is null)
            {
                return Results.NotFound();
            }

            // The row is trusted content, but a redirect off-site is exactly what a tampered row
            // would produce, so the stored path is checked the same way a save checks it.
            if (!guard.IsSiteFilePath(path))
            {
                loggers.CreateLogger("Triathlon.Web.Api.Downloads").LogWarning("{Kind} {Id} has a non-site file path and was not served.", kind, id);
                return Results.NotFound();
            }

            return Results.Redirect(path);
        })
        .CacheOutput(policy => policy.NoCache());

    /// <summary>An unknown value is no filter at all, the same reading the events list gives it.</summary>
    private static EventType? ParseType(string? type) => type?.ToLowerInvariant() switch
    {
        "competition" => EventType.Competition,
        "community" => EventType.Community,
        _ => null,
    };
}
