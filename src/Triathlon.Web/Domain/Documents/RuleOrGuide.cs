using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Documents;

/// <summary>One entry on the rules page — a rulebook, a handbook, or a policy document.</summary>
public sealed class RuleOrGuide : BaseEntity
{
    /// <summary>URL segment, unique.</summary>
    public required string Slug { get; set; }

    public required string TitleEn { get; set; }
    public required string TitleAr { get; set; }
    public required string DescriptionEn { get; set; }
    public required string DescriptionAr { get; set; }

    public RuleAudience Audience { get; set; }

    /// <summary>Public URL path, e.g. <c>/docs/competition-rules-2026.pdf</c>.</summary>
    public required string FilePath { get; set; }

    public long FileSize { get; set; }

    public DateOnly UpdatedOn { get; set; }

    public int Downloads { get; set; }

    public bool IsPublished { get; set; }

    public int SortOrder { get; set; }
}
