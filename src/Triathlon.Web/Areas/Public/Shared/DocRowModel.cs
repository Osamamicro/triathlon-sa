using System.Globalization;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Areas.Public;

/// <summary>
/// One downloadable file as the <c>_DocRow</c> partial renders it. The three tables that produce a
/// row — documents, rules and training guides — have nothing in common but this shape, so the
/// mapping from each of them lives here rather than being repeated in four views.
/// <para>
/// <see cref="Href"/> is always the counting redirect (<c>/documents/{id}/download</c> and its
/// siblings), never the file itself, which is why the anchor carries no <c>download</c> attribute:
/// the endpoint records the download and then redirects to the PDF.
/// </para>
/// </summary>
/// <param name="MetaLeftEn">The first meta cell — a category, or "UPDATED 2026-06".</param>
/// <param name="MetaMid">The optional middle meta cell; a year, when the row has one.</param>
/// <param name="Size">
/// Already formatted for the culture by <see cref="PublicText.FileSize"/>; <c>null</c> when the
/// file's size is not known, in which case <c>_DocRow</c> omits the size cell rather than showing
/// a false "0 KB".
/// </param>
/// <param name="HeadingLevel">
/// The row title's heading level. <c>_DocRow</c> is shared by pages whose document list sits at a
/// different depth in the outline — straight under the page's &lt;h1&gt;, under one &lt;h2&gt;
/// section, or nested inside an &lt;h3&gt; card — so the caller states the level that keeps the
/// page's headings sequential instead of every list defaulting to the same tag. Defaults to the
/// depth <c>Training/Index.cshtml</c> needs (a guide card's &lt;h3&gt; followed by its row).
/// </param>
public sealed record DocRowModel(
    string Href,
    string TitleEn,
    string TitleAr,
    string MetaLeftEn,
    string MetaLeftAr,
    string? MetaMid,
    string? Size,
    string? DescEn = null,
    string? DescAr = null,
    int HeadingLevel = 4)
{
    /// <summary>A library document: category on the left, year in the middle.</summary>
    public static DocRowModel For(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var (labelEn, labelAr) = CategoryLabel(document.Category);

        return new DocRowModel(
            Href: $"/documents/{document.Id}/download",
            TitleEn: document.TitleEn,
            TitleAr: document.TitleAr,
            MetaLeftEn: labelEn,
            MetaLeftAr: labelAr,
            MetaMid: PublicText.Digits(document.Year.ToString(CultureInfo.InvariantCulture)),
            Size: PublicText.FileSize(document.FileSize));
    }

    /// <summary>A rule or handbook: its description under the title, and the month it was last revised.</summary>
    public static DocRowModel For(RuleOrGuide rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var month = rule.UpdatedOn.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        return new DocRowModel(
            Href: $"/rules/{rule.Id}/download",
            TitleEn: rule.TitleEn,
            TitleAr: rule.TitleAr,
            MetaLeftEn: "UPDATED " + month,
            MetaLeftAr: "تحديث " + PublicText.Digits(month),
            MetaMid: null,
            Size: PublicText.FileSize(rule.FileSize),
            DescEn: rule.DescriptionEn,
            DescAr: rule.DescriptionAr);
    }

    /// <summary>
    /// A training guide's PDF. Only a published guide with a file has one, so the caller checks
    /// <see cref="TrainingGuide.FilePath"/> before asking for the row.
    /// </summary>
    public static DocRowModel For(TrainingGuide guide)
    {
        ArgumentNullException.ThrowIfNull(guide);

        return new DocRowModel(
            Href: $"/training/{guide.Id}/download",
            TitleEn: guide.TitleEn,
            TitleAr: guide.TitleAr,
            MetaLeftEn: guide.LevelEn.ToUpperInvariant(),
            MetaLeftAr: guide.LevelAr,
            MetaMid: null,
            Size: guide.FileSize is { } size ? PublicText.FileSize(size) : null);
    }

    /// <summary>The library's filing categories as the prototype labelled them.</summary>
    public static (string En, string Ar) CategoryLabel(DocumentCategory category) => category switch
    {
        DocumentCategory.Governance => ("GOVERNANCE", "حوكمة"),
        DocumentCategory.Finance => ("FINANCIAL", "مالية"),
        DocumentCategory.Minutes => ("MINUTES", "محاضر"),
        _ => (category.ToString().ToUpperInvariant(), category.ToString()),
    };
}
