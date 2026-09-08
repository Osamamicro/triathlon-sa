using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Services;

/// <summary>The dashboard's write-side shapes: what an editor submits, before it becomes a tracked entity.</summary>
public sealed record BlockInput(Guid? Id, int SortOrder, BlockType Type, string? Variant, string? Anchor,
    string? EyebrowEn, string? EyebrowAr, string? TitleEn, string? TitleAr, string? BodyEn, string? BodyAr,
    IReadOnlyList<BlockItem> Items, string? CtaLabelEn, string? CtaLabelAr, string? CtaHref,
    string? SecondaryLabelEn, string? SecondaryLabelAr, string? SecondaryHref);

public sealed record PageInput(string Slug, string TitleEn, string TitleAr, string? MetaDescriptionEn, string? MetaDescriptionAr, bool IsPublished, IReadOnlyList<BlockInput> Blocks);

public sealed record NavItemInput(Guid? Id, string LabelEn, string LabelAr, string Href, bool IsPublished);

public sealed record CommitteeInput(Guid? Id, string KindEn, string KindAr, string NameEn, string NameAr, string DescriptionEn, string DescriptionAr, int SortOrder, bool IsPublished);

public sealed record ClubInput(Guid? Id, string NameEn, string NameAr, string CityEn, string CityAr, bool IsActive, int SortOrder);

public sealed record SiteSettingInput(string Key, string ValueEn, string ValueAr);

public sealed record NewsPostInput(Guid? Id, string Slug, string TitleEn, string TitleAr, string SummaryEn, string SummaryAr, string BodyEn, string BodyAr, string? HeroImagePath, DateOnly PublishedOn, bool IsPublished);
