using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Content;

/// <summary>
/// One link in the header bar or a footer column. The navigation is content, so renaming a section
/// is an edit rather than a deployment.
/// </summary>
public sealed class NavItem : BaseEntity
{
    public NavLocation Location { get; set; }

    public required string LabelEn { get; set; }
    public required string LabelAr { get; set; }

    /// <summary>
    /// A path inside the culture (<c>events</c>, <c>join#clubs</c>, or the empty string for the home
    /// page) or an absolute URL; resolved for rendering by <c>PublicText.Href</c>.
    /// </summary>
    public required string Href { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }
}
