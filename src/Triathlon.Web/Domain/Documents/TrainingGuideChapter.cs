using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Documents;

/// <summary>One chapter of a <see cref="TrainingGuide"/>. Owned by its guide — deleting the guide
/// deletes its chapters (<c>Cascade</c>).</summary>
public sealed class TrainingGuideChapter : BaseEntity
{
    public Guid TrainingGuideId { get; set; }

    public int SortOrder { get; set; }

    public required string TitleEn { get; set; }
    public required string TitleAr { get; set; }

    /// <summary>HTML written by editors — see the plan's Global Constraints on sanitising on save.</summary>
    public required string BodyEn { get; set; }
    public required string BodyAr { get; set; }
}
