using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Stats;

/// <summary>
/// Registered athletes by region, for the statistics page's "Athletes by region" chart
/// (prototype <c>assets/js/data.js</c> <c>regionAthletes</c>, lines 23–30).
/// </summary>
public sealed class RegionStat : BaseEntity
{
    /// <summary>Stable machine key, e.g. <c>"riyadh"</c>. Unique.</summary>
    public required string Key { get; set; }

    public required string NameEn { get; set; }
    public required string NameAr { get; set; }

    public int Athletes { get; set; }

    public int SortOrder { get; set; }
}
