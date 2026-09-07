using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Events;

/// <summary>
/// One race or community activity. Status is derived (<see cref="StatusOn"/>), not stored, so
/// "today" moving forward is the only thing that changes it. Deleting an event soft-deletes its
/// owned <see cref="Gallery"/> and <see cref="Results"/> but keeps <see cref="Registrations"/> —
/// see <c>docs/adr/0001-soft-delete-cascade.md</c>.
/// </summary>
public sealed class Event : BaseEntity
{
    /// <summary>
    /// URL segment, unique, <c>[a-z0-9-]</c> only (lower-case letters, digits, hyphens — no dots).
    /// A dot would make <c>PublicSite.WantsHtmlStatusPage</c>'s <c>Path.HasExtension</c> check treat
    /// the URL as a static file request and serve a bare 404 instead of the branded page. Slugs are
    /// seeded now and edited as plain text by the dashboard from Week 4; no validation attribute
    /// exists yet, so this rule is enforced by review until then.
    /// </summary>
    public required string Slug { get; set; }

    public EventType Type { get; set; }

    /// <summary>Draft/publish, independent of <see cref="StatusOn"/>.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Federation season label, e.g. <c>"2026-27"</c>.</summary>
    public required string Season { get; set; }

    public DateOnly DateStart { get; set; }
    public DateOnly? DateEnd { get; set; }
    public TimeOnly? StartTime { get; set; }

    public Guid CityId { get; set; }
    public City City { get; set; } = null!;

    public required string TitleEn { get; set; }
    public required string TitleAr { get; set; }
    public required string VenueEn { get; set; }
    public required string VenueAr { get; set; }
    public required string DescriptionEn { get; set; }
    public required string DescriptionAr { get; set; }

    public string? SwimDistance { get; set; }
    public string? BikeDistance { get; set; }
    public string? RunDistance { get; set; }

    /// <summary>Comma-separated, e.g. <c>"Elite,Age Group,Junior"</c>; see <see cref="CategoryList"/>.</summary>
    public string Categories { get; set; } = "";

    public RegistrationMode RegistrationMode { get; set; }
    public bool RegistrationOpen { get; set; }
    public string? ExternalRegistrationUrl { get; set; }
    public int? Capacity { get; set; }

    public string? HeroImagePath { get; set; }
    public string? ResultsFilePath { get; set; }

    public List<EventGalleryImage> Gallery { get; set; } = [];
    public List<EventResult> Results { get; set; } = [];
    public List<EventRegistration> Registrations { get; set; } = [];

    /// <summary><see cref="Categories"/> split into display chips.</summary>
    public IReadOnlyList<string> CategoryList =>
        Categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Completed once the start date is behind "today" (the race day itself stays upcoming until
    /// midnight, like the prototype); otherwise open only while registration is actually available.
    /// "Today" is always the Riyadh date — see <see cref="Services.EventsService.Today"/>.
    /// </summary>
    public EventStatus StatusOn(DateOnly today) =>
        DateStart < today ? EventStatus.Completed
        : RegistrationMode != RegistrationMode.None && RegistrationOpen ? EventStatus.Open
        : EventStatus.OpensSoon;
}
