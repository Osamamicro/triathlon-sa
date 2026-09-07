using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Crm;

/// <summary>An affiliated club, listed on the join page and offered in the registration form.</summary>
public sealed class Club : BaseEntity
{
    public required string NameEn { get; set; }
    public required string NameAr { get; set; }

    public required string CityEn { get; set; }
    public required string CityAr { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
