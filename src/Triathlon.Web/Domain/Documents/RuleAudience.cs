namespace Triathlon.Web.Domain.Documents;

/// <summary>
/// Who a rule or guide is written for. Combinable — <c>competition-rules-2026</c> is
/// <see cref="Athletes"/> | <see cref="Organizers"/> | <see cref="Officials"/> — so the rules page's
/// audience filter matches on any overlapping bit, not an exact set.
/// </summary>
[Flags]
public enum RuleAudience
{
    None = 0,
    Athletes = 1,
    Organizers = 2,
    Officials = 4,
    Coaches = 8,
}
