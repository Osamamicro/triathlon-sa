using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Crm;

/// <summary>
/// Someone who asked the federation for a licence. Created as <see cref="AthleteStatus.Pending"/>
/// by the public registration form; the membership desk approves the record and issues the licence
/// number. National ID, emergency contact and licence issuing arrive with the CRM in Week 5.
/// </summary>
public sealed class Athlete : BaseEntity
{
    public required string FullName { get; set; }

    public required string Email { get; set; }

    public DateOnly DateOfBirth { get; set; }

    /// <summary>The key of the city the athlete trains in, when they named one.</summary>
    public string? CityKey { get; set; }

    /// <summary>The category chosen on the form: youth, age group, elite, para, community.</summary>
    public required string Category { get; set; }

    public Guid? ClubId { get; set; }

    /// <summary>The event that brought them here, when they registered from one.</summary>
    public Guid? InterestEventId { get; set; }

    public AthleteStatus Status { get; set; }

    /// <summary>Issued on approval. Week 5 adds the uniqueness index along with the issuing flow.</summary>
    public string? LicenceNumber { get; set; }

    public DateTimeOffset ConsentAt { get; set; }

    /// <summary>The language the athlete registered in, so their mail arrives in it.</summary>
    public string PreferredCulture { get; set; } = "en";
}
