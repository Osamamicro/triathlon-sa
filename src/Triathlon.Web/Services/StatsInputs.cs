using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Services;

/// <summary>
/// What an editor posts to update one KPI tile. The key set is structural — the ten rows are
/// created once by <c>SeedStats</c> and never from the dashboard — so a save always targets an
/// existing row by <see cref="Id"/> rather than a <see cref="Kpi.Key"/> the editor could invent.
/// </summary>
public sealed record KpiInput(
    Guid Id, string LabelEn, string LabelAr, long Value, string? Suffix, bool ShowPlus,
    string? NoteEn, string? NoteAr, string? Color, int SortOrder, bool ShowOnHome, int HomeOrder, KpiSource Source);

/// <summary>What an editor posts to save one region-athlete row, as part of a whole-list replace.</summary>
public sealed record RegionInput(Guid? Id, string Key, string NameEn, string NameAr, int Athletes, int SortOrder);

/// <summary>What an editor posts to save one growth-series year, as part of a whole-list replace.</summary>
public sealed record GrowthInput(Guid? Id, int Year, int Athletes);
