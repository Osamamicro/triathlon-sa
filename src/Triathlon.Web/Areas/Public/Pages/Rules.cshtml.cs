using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// The regulations library, filtered by who a document is written for, followed by whatever the
/// editors have put on the <c>rules</c> page — the race-day quick reference today. The filter is a
/// query key, so the page is tagged to vary on it rather than serving one audience's list to the
/// next.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Rules], VaryByQueryKeys = ["audience"])]
public sealed class RulesModel(DocumentsService documents, ContentService content) : PageModel
{
    public RuleAudience? Audience { get; private set; }

    public IReadOnlyList<RuleOrGuide> Rules { get; private set; } = [];

    /// <summary>The blocks of the <c>rules</c> CMS page, rendered under the library.</summary>
    public IReadOnlyList<PageBlock> Trailing { get; private set; } = [];

    public async Task OnGetAsync(string? audience, CancellationToken ct)
    {
        Audience = ParseAudience(audience);
        Rules = await documents.RulesAsync(Audience, ct);
        Trailing = (await content.PageAsync("rules", ct))?.Blocks ?? [];

        ViewData["Title"] = PublicText.Bi("Rules & Regulations", "اللوائح والأنظمة");
        ViewData["MetaDescription"] = PublicText.Bi(
            "The Federation's competition rules and technical regulations for athletes, organizers, officials and coaches, plus a race-day quick reference.",
            "قوانين المنافسات واللوائح الفنية للاتحاد الموجهة للرياضيين والمنظمين والحكام والمدربين، مع مرجع سريع ليوم السباق.");

        // The page's own tag is built from a slug, so it goes on the cache entry here: an
        // [OutputCache] tag has to be a compile-time constant.
        HttpContext.Features.Get<IOutputCacheFeature>()?.Context.Tags.Add(CacheTags.Page("rules"));
    }

    /// <summary>The audience as it appears in the query string.</summary>
    public static string Key(RuleAudience audience) => audience.ToString().ToLowerInvariant();

    /// <summary>The library under a different audience, keeping the culture it is rendered in.</summary>
    public static string FilterUrl(RuleAudience? audience) =>
        PublicCulture.Url(PublicText.Culture, "rules") + (audience is { } value ? "?audience=" + Key(value) : "");

    /// <summary>An unknown value is no filter at all, the same reading the events list gives it.</summary>
    private static RuleAudience? ParseAudience(string? audience) => audience?.ToLowerInvariant() switch
    {
        "athletes" => RuleAudience.Athletes,
        "organizers" => RuleAudience.Organizers,
        "officials" => RuleAudience.Officials,
        "coaches" => RuleAudience.Coaches,
        _ => null,
    };
}
