using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Services;

/// <summary>What an editor posts to save one entry in the governance & documents library.</summary>
public sealed record DocumentInput(
    Guid? Id, string TitleEn, string TitleAr, DocumentCategory Category, int Year,
    string FilePath, long FileSize, bool IsPublished, int SortOrder);

/// <summary>What an editor posts to save one rulebook/handbook entry on the rules page.</summary>
public sealed record RuleInput(
    Guid? Id, string Slug, string TitleEn, string TitleAr, string DescriptionEn, string DescriptionAr,
    RuleAudience Audience, string FilePath, long FileSize, DateOnly UpdatedOn, bool IsPublished, int SortOrder);

/// <summary>One chapter of a training guide, as an editor posts it.</summary>
public sealed record ChapterInput(Guid? Id, string TitleEn, string TitleAr, string BodyEn, string BodyAr);

/// <summary>
/// What an editor posts to save one training guide. <see cref="FilePath"/>/<see cref="FileSize"/>
/// are nullable — a guide can publish on its chapters alone, with no PDF underneath — but a guide
/// with neither a file nor a chapter cannot be published (see <c>DocumentsService.SaveGuideAsync</c>).
/// </summary>
public sealed record GuideInput(
    Guid? Id, string Slug, string TitleEn, string TitleAr, string SummaryEn, string SummaryAr,
    string LevelEn, string LevelAr, string? FilePath, long? FileSize, bool IsPublished, int SortOrder,
    IReadOnlyList<ChapterInput> Chapters);
