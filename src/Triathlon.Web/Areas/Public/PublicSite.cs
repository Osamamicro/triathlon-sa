using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Net.Http.Headers;

namespace Triathlon.Web.Areas.Public;

/// <summary>
/// Wiring for the public website: one set of Razor Pages served under a culture segment.
/// <para>
/// Pages live in the <c>Public</c> area, so the framework routes them as <c>/Public/{page}</c>.
/// The convention below rewrites that area prefix into a constrained <c>{culture}</c> segment, so
/// <c>Areas/Public/Pages/Index.cshtml</c> answers <c>/en</c> and <c>/ar</c> and every future page
/// gets its culture prefix for free. The same segment feeds request localization, which is why the
/// route provider is the only culture provider registered: an <c>Accept-Language</c> header or a
/// stale cookie must never disagree with the URL the visitor is looking at.
/// </para>
/// </summary>
public static class PublicSite
{
    public const string AreaName = "Public";
    public const string DefaultCulture = "en";

    /// <summary>The cultures the public site is published in, in preference order.</summary>
    public static readonly string[] SupportedCultures = ["en", "ar"];

    /// <summary>
    /// The route segment every public URL starts with. Plain "ar" rather than "ar-SA" on purpose:
    /// ar-SA's default calendar is Umm al-Qura, and the site shows Gregorian dates in both languages.
    /// </summary>
    private const string CultureSegment = "{culture:regex(^(en|ar)$)}";

    public static IServiceCollection AddPublicSite(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        // The default HTML encoder escapes everything outside Basic Latin, which would turn every
        // localized Arabic string into a wall of &#x0627; entities. Arabic is a first-class language
        // here, so its ranges are emitted literally; the markup-significant characters still escape.
        services.AddWebEncoders(options => options.TextEncoderSettings = new TextEncoderSettings(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.Latin1Supplement,
            UnicodeRanges.GeneralPunctuation,
            UnicodeRanges.Arabic,
            UnicodeRanges.ArabicSupplement,
            UnicodeRanges.ArabicExtendedA,
            UnicodeRanges.ArabicPresentationFormsA,
            UnicodeRanges.ArabicPresentationFormsB));

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedCultures.Select(c => new CultureInfo(c)).ToArray();
            options.DefaultRequestCulture = new RequestCulture(DefaultCulture);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.ApplyCurrentCultureToResponseHeaders = true;

            // The URL is the single source of truth for language; nothing else gets a vote.
            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new RouteDataRequestCultureProvider
            {
                RouteDataStringKey = "culture",
                UIRouteDataStringKey = "culture",
            });
        });

        services.AddRazorPages(options =>
            options.Conventions.AddAreaFolderRouteModelConvention(AreaName, "/", UseCultureSegment));

        // The layout, header and footer sit in Areas/Public/Shared rather than under Pages, so the
        // page view engine has to be told to look there when resolving them by name.
        services.Configure<RazorViewEngineOptions>(options =>
            options.AreaPageViewLocationFormats.Add("/Areas/{2}/Shared/{0}" + RazorViewEngine.ViewExtension));

        return services;
    }

    /// <summary>
    /// Sends a visitor who lands on the bare origin to the culture they asked for. This is the only
    /// public URL without a culture segment, and it never renders anything itself.
    /// </summary>
    public static IEndpointConventionBuilder MapPublicRoot(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", (HttpRequest request) => Results.Redirect("/" + PreferredCulture(request)));

    /// <summary>
    /// The visitor's highest-weighted <c>Accept-Language</c> entry that the site is actually
    /// published in, or English when the header is absent, unparseable or asks for neither.
    /// </summary>
    internal static string PreferredCulture(HttpRequest request)
    {
        if (!StringWithQualityHeaderValue.TryParseList(request.Headers.AcceptLanguage, out var languages))
        {
            return DefaultCulture;
        }

        // OrderByDescending is stable, so entries of equal quality keep the order the client sent.
        foreach (var language in languages.OrderByDescending(l => l.Quality ?? 1d))
        {
            var match = SupportedCultures.FirstOrDefault(c =>
                language.Value.StartsWith(c, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }
        }

        return DefaultCulture;
    }

    private static void UseCultureSegment(PageRouteModel model)
    {
        foreach (var selector in model.Selectors)
        {
            var template = selector.AttributeRouteModel?.Template;
            if (template is null)
            {
                continue;
            }

            // "Public" (the area's index) and "Public/whatever" are the only shapes the page route
            // model factory produces for an area, and both lose the area name to the culture.
            if (template.Equals(AreaName, StringComparison.OrdinalIgnoreCase))
            {
                selector.AttributeRouteModel!.Template = CultureSegment;
            }
            else if (template.StartsWith(AreaName + "/", StringComparison.OrdinalIgnoreCase))
            {
                selector.AttributeRouteModel!.Template = CultureSegment + template[AreaName.Length..];
            }
        }
    }
}
