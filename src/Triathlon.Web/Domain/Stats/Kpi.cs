using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Stats;

public enum KpiSource { Manual = 0, Computed = 1 }

/// <summary>
/// One federation indicator ("athletes", "elite", …), transcribed from the prototype's
/// <c>assets/js/data.js</c> <c>stats</c> object (lines 10–21).
/// <para>
/// <see cref="HomeOrder"/> is deliberately independent of <see cref="SortOrder"/>: the statistics
/// page lists all ten KPIs in the prototype's original order (<see cref="SortOrder"/> 1..10), while
/// the home page shows only four of them, in their own order (athletes, elite, tournaments,
/// participants) — a KPI can be promoted to, or reordered within, the home band without touching
/// where it sits on the statistics page.
/// </para>
/// </summary>
public sealed class Kpi : BaseEntity
{
    /// <summary>Stable machine key, e.g. <c>"athletes"</c>. Unique.</summary>
    public required string Key { get; set; }

    public required string LabelEn { get; set; }
    public required string LabelAr { get; set; }

    public long Value { get; set; }

    /// <summary>Unit appended after the animated counter settles, e.g. <c>"%"</c>.</summary>
    public string? Suffix { get; set; }

    /// <summary>Whether a trailing "+" renders after the number, e.g. <c>"1,284+"</c>.</summary>
    public bool ShowPlus { get; set; }

    public string? NoteEn { get; set; }
    public string? NoteAr { get; set; }

    /// <summary><c>"swim"</c> | <c>"bike"</c> | <c>"run"</c>, or null to let the view cycle a colour by position.</summary>
    public string? Color { get; set; }

    /// <summary>Order on the statistics page (1..10, the prototype's own order).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this KPI appears in the home page's four-tile band.</summary>
    public bool ShowOnHome { get; set; }

    /// <summary>Position (1..4) within the home band; 0 when <see cref="ShowOnHome"/> is false.</summary>
    public int HomeOrder { get; set; }

    /// <summary>
    /// Reserved for the Week 4 nightly computation job; every KPI is <see cref="KpiSource.Manual"/>
    /// today and this field is display-only.
    /// </summary>
    public KpiSource Source { get; set; }
}
