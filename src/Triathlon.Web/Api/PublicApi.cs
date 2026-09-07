using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Triathlon.Web.Areas.Public;
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

        api.MapPost("/events/{slug}/register", async (
            string slug,
            [FromForm] GuestEntryForm form,
            EventsService events,
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

            if (!Validator.TryValidateObject(form, new ValidationContext(form), null, validateAllProperties: true))
            {
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

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
