using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Documents;

/// <summary>One entry in the governance & documents library — an annual report, a financial
/// statement, or a set of board minutes.</summary>
public sealed class Document : BaseEntity
{
    public required string TitleEn { get; set; }
    public required string TitleAr { get; set; }

    public DocumentCategory Category { get; set; }

    public int Year { get; set; }

    /// <summary>Public URL path, e.g. <c>/docs/annual-report-2025.pdf</c>.</summary>
    public required string FilePath { get; set; }

    public long FileSize { get; set; }

    public int Downloads { get; set; }

    public bool IsPublished { get; set; }

    public int SortOrder { get; set; }
}
