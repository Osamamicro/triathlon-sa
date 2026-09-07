using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Documents;

/// <summary>
/// One training guide. An unpublished guide is an "in preparation" roadmap card with no chapters
/// and no file yet — see <see cref="TrainingGuideChapter"/> and the seed's three placeholder guides.
/// </summary>
public sealed class TrainingGuide : BaseEntity
{
    /// <summary>URL segment, unique.</summary>
    public required string Slug { get; set; }

    public required string TitleEn { get; set; }
    public required string TitleAr { get; set; }
    public required string SummaryEn { get; set; }
    public required string SummaryAr { get; set; }

    /// <summary>e.g. "Beginner" / "للمبتدئين".</summary>
    public required string LevelEn { get; set; }
    public required string LevelAr { get; set; }

    public string? FilePath { get; set; }
    public long? FileSize { get; set; }

    public int Downloads { get; set; }

    /// <summary>False renders as an "in preparation" roadmap card instead of a servable guide.</summary>
    public bool IsPublished { get; set; }

    public int SortOrder { get; set; }

    public List<TrainingGuideChapter> Chapters { get; set; } = [];
}
