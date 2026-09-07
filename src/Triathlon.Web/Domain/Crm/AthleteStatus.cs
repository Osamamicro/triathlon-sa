namespace Triathlon.Web.Domain.Crm;

/// <summary>Where a registered athlete stands with the federation's membership desk.</summary>
public enum AthleteStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Suspended = 3,
}
