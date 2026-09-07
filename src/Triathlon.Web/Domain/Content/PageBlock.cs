using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Content;

/// <summary>
/// One section of a <see cref="Page"/>. The fixed columns cover what every block shows — an
/// eyebrow, a title, a body, up to two calls to action — and anything that repeats lives in
/// <see cref="ItemsJson"/>.
/// </summary>
public sealed class PageBlock : BaseEntity
{
    public Guid PageId { get; set; }

    public int SortOrder { get; set; }

    public BlockType Type { get; set; }

    /// <summary>A free string the block's partial understands, e.g. <c>grid-4</c> or <c>pathway</c>.</summary>
    public string? Variant { get; set; }

    /// <summary>The section's <c>id</c> attribute, so a link such as <c>join#clubs</c> lands on it.</summary>
    public string? Anchor { get; set; }

    public string? EyebrowEn { get; set; }
    public string? EyebrowAr { get; set; }

    public string? TitleEn { get; set; }
    public string? TitleAr { get; set; }

    /// <summary>Editor-written HTML for the rich-text, CTA and hero-lead blocks.</summary>
    public string? BodyEn { get; set; }
    public string? BodyAr { get; set; }

    /// <summary>A serialised <see cref="BlockItem"/> list; see <see cref="Items"/>.</summary>
    public string ItemsJson { get; set; } = "[]";

    public string? CtaLabelEn { get; set; }
    public string? CtaLabelAr { get; set; }
    public string? CtaHref { get; set; }

    public string? SecondaryLabelEn { get; set; }
    public string? SecondaryLabelAr { get; set; }
    public string? SecondaryHref { get; set; }

    /// <summary>
    /// Parsed on each read rather than cached: a block is rendered once per cached response, and a
    /// cached list would have to be invalidated whenever the dashboard rewrites the JSON.
    /// </summary>
    public IReadOnlyList<BlockItem> Items => BlockItem.Parse(ItemsJson);
}
