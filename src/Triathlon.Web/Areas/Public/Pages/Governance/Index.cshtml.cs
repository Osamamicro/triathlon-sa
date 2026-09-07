using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages.Governance;

/// <summary>
/// How the federation is governed, and the newest of what it publishes. The full library lives one
/// click away on <see cref="DocumentsModel"/>; this page carries the six most recent files so that
/// the answer to "where are the minutes?" is on the section's front page.
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Governance, CacheTags.Documents])]
public sealed class IndexModel(ContentService content, DocumentsService documents) : PageModel
{
    /// <summary>How many of the newest documents the section's front page lists.</summary>
    private const int RecentDocuments = 6;

    public IReadOnlyList<Committee> Committees { get; private set; } = [];

    public IReadOnlyList<Document> Recent { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Committees = await content.CommitteesAsync(ct);

        // QueryAsync already orders newest year first, then by the editor's own order.
        Recent = (await documents.QueryAsync(null, null, ct)).Take(RecentDocuments).ToList();

        ViewData["Title"] = PublicText.Bi("Governance & Transparency", "الحوكمة والشفافية");
        ViewData["MetaDescription"] = PublicText.Bi(
            "How the Saudi Triathlon Federation is governed: the board, its committees, and the newest bylaws, financial statements and meeting minutes it has published.",
            "كيف يُدار الاتحاد السعودي للترايثلون: مجلس الإدارة ولجانه، وأحدث اللوائح والقوائم المالية ومحاضر الاجتماعات المنشورة.");
    }
}
