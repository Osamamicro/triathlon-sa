using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Services;

/// <summary>One gallery photo as an editor posts it. See <see cref="EventInput"/>.</summary>
public sealed record GalleryImageInput(Guid? Id, string Path, string? AltEn, string? AltAr);

/// <summary>One finisher row as an editor posts it. See <see cref="EventInput"/>.</summary>
public sealed record EventResultInput(Guid? Id, int Position, string AthleteEn, string AthleteAr, string? ClubEn, string? ClubAr, string Time);

/// <summary>An event save, gallery and results included — the dashboard's one form for create and update.</summary>
public sealed record EventInput(
    string Slug, EventType Type, bool IsPublished, string Season, DateOnly DateStart, DateOnly? DateEnd, TimeOnly? StartTime,
    Guid CityId, string TitleEn, string TitleAr, string VenueEn, string VenueAr, string DescriptionEn, string DescriptionAr,
    string? SwimDistance, string? BikeDistance, string? RunDistance, IReadOnlyList<string> Categories,
    RegistrationMode RegistrationMode, bool RegistrationOpen, string? ExternalRegistrationUrl, int? Capacity,
    string? HeroImagePath, string? ResultsFilePath, IReadOnlyList<GalleryImageInput> Gallery, IReadOnlyList<EventResultInput> Results);

/// <summary>A host city save, as posted from the cities editor.</summary>
public sealed record CityInput(Guid? Id, string Key, string NameEn, string NameAr, int SvgX, int SvgY, bool LabelAtEnd, int LabelDy, int SortOrder);
