using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Content;

/// <summary>
/// One editable page of the public site, rendered as its ordered list of blocks. Dedicated pages
/// (rules, training, governance, statistics) render their own aggregate first and then the blocks
/// of the page with the same slug; everything else is served by the generic content page.
/// </summary>
public sealed class Page : BaseEntity
{
    /// <summary>URL segment, unique — <c>home</c>, <c>join</c>, <c>contact</c>, <c>rules</c>, <c>training</c>.</summary>
    public required string Slug { get; set; }

    public required string TitleEn { get; set; }
    public required string TitleAr { get; set; }

    public string? MetaDescriptionEn { get; set; }
    public string? MetaDescriptionAr { get; set; }

    public bool IsPublished { get; set; }

    public List<PageBlock> Blocks { get; set; } = [];
}
