using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Stats;

/// <summary>
/// One year's registered-athlete count, for the statistics page's "Registered athletes by year"
/// growth chart (prototype <c>assets/js/data.js</c> <c>growth</c>, lines 32–37).
/// </summary>
public sealed class GrowthPoint : BaseEntity
{
    /// <summary>Unique.</summary>
    public int Year { get; set; }

    public int Athletes { get; set; }
}
