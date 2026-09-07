using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Content;

/// <summary>One card in the federation's governance structure — the board, or one of its committees.</summary>
public sealed class Committee : BaseEntity
{
    /// <summary>The card's eyebrow: "Board" / "المجلس".</summary>
    public required string KindEn { get; set; }
    public required string KindAr { get; set; }

    public required string NameEn { get; set; }
    public required string NameAr { get; set; }

    public required string DescriptionEn { get; set; }
    public required string DescriptionAr { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }
}
