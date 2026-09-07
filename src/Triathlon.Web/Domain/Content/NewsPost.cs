using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Content;

/// <summary>
/// One news article. <see cref="PublishedOn"/> is a date rather than a timestamp because the
/// newsroom thinks in days, and a post dated ahead of today in Riyadh stays off the site until that
/// date arrives — scheduling with no job to run it.
/// </summary>
public sealed class NewsPost : BaseEntity
{
    /// <summary>URL segment, unique.</summary>
    public required string Slug { get; set; }

    public required string TitleEn { get; set; }
    public required string TitleAr { get; set; }

    public required string SummaryEn { get; set; }
    public required string SummaryAr { get; set; }

    /// <summary>Editor-written HTML.</summary>
    public required string BodyEn { get; set; }
    public required string BodyAr { get; set; }

    public string? HeroImagePath { get; set; }

    public DateOnly PublishedOn { get; set; }

    public bool IsPublished { get; set; }
}
