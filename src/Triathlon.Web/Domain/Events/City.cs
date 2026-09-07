using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Events;

/// <summary>
/// A host city on the Kingdom map. Positions and label placement are the prototype's own SVG
/// coordinates (<c>assets/js/data.js</c> <c>cities</c>), carried over unchanged so the timeline's
/// map renders identically once it reads from the database.
/// </summary>
public sealed class City : BaseEntity
{
    /// <summary>Stable machine key, e.g. <c>"riyadh"</c>. Unique, lower-case.</summary>
    public required string Key { get; set; }

    public required string NameEn { get; set; }
    public required string NameAr { get; set; }

    public int SvgX { get; set; }
    public int SvgY { get; set; }

    /// <summary>Prototype's <c>anchor: "end"</c> — the label sits to the left of the pin instead of the right.</summary>
    public bool LabelAtEnd { get; set; }

    /// <summary>Prototype's label <c>dy</c> — vertical nudge for the city name text.</summary>
    public int LabelDy { get; set; }

    public int SortOrder { get; set; }
}
