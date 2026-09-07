using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Governance;

/// <summary>
/// The documents library. Both filters are query-string state rather than script, so every
/// category-and-year combination is a shareable URL and a cacheable response — hence
/// <c>VaryByQueryKeys</c> on exactly the two keys the page reads.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Documents], VaryByQueryKeys = ["category", "year"])]
public sealed class DocumentsModel(DocumentsService documents) : PageModel
{
    public DocumentCategory? Category { get; private set; }

    public int? Year { get; private set; }

    public IReadOnlyList<Document> Documents { get; private set; } = [];

    public DocumentFacets Facets { get; private set; } = new(new Dictionary<DocumentCategory, int>(), new Dictionary<int, int>());

    /// <summary>The years that have documents, newest first, for the year picker.</summary>
    public IReadOnlyList<int> Years { get; private set; } = [];

    public async Task OnGetAsync(string? category, int? year, CancellationToken ct)
    {
        Category = ParseCategory(category);

        Facets = await documents.FacetsAsync(ct);
        Years = Facets.Years.Keys.OrderByDescending(y => y).ToList();

        // A year nobody published in is treated as no filter rather than as an empty page: a stale
        // link should show the library, not an apology.
        Year = year is { } requested && Facets.Years.ContainsKey(requested) ? requested : null;

        Documents = await documents.QueryAsync(Category, Year, ct);

        ViewData["Title"] = PublicText.Bi("Documents Library", "مكتبة المستندات");
    }

    /// <summary>The category as it appears in the query string, so the year form can carry it.</summary>
    public string? CategoryKey => Category?.ToString().ToLowerInvariant();

    /// <summary>The library under a different filter, keeping the culture it is rendered in.</summary>
    public static string FilterUrl(DocumentCategory? category, int? year)
    {
        var query = new List<string>();
        if (category is { } value)
        {
            query.Add("category=" + value.ToString().ToLowerInvariant());
        }

        if (year is { } chosen)
        {
            query.Add("year=" + chosen.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return PublicCulture.Url(PublicText.Culture, "governance/documents")
               + (query.Count > 0 ? "?" + string.Join('&', query) : "");
    }

    /// <summary>An unknown value is no filter at all, the same reading the events list gives it.</summary>
    private static DocumentCategory? ParseCategory(string? category) => category?.ToLowerInvariant() switch
    {
        "governance" => DocumentCategory.Governance,
        "finance" => DocumentCategory.Finance,
        "minutes" => DocumentCategory.Minutes,
        _ => null,
    };
}
