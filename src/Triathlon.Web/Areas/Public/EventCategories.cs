namespace Triathlon.Web.Areas.Public;

/// <summary>
/// Bilingual labels for the free-text category strings stored on <see cref="Domain.Events.Event.Categories"/>
/// (<c>SeedEvents.cs</c>, e.g. <c>"Elite,Age Group,Junior"</c>). Unlike <see cref="Triathlon.Web.Services.AthleteCategories"/>,
/// which is a closed enum the athlete-registration endpoint validates against, an event's categories are
/// free text an editor can type in the dashboard — so this lookup only supplies a display label and
/// falls back to the raw value for anything it does not recognise, rather than restricting the set.
/// The stored English value is always what a form posts and what the endpoint checks
/// (<c>PublicApi.cs</c>'s <c>ev.CategoryList.Contains(form.Category, ...)</c>); only the visible text changes.
/// </summary>
public static class EventCategories
{
    private static readonly Dictionary<string, string> ArabicLabels = new(StringComparer.Ordinal)
    {
        ["Elite"] = "النخبة",
        ["Age Group"] = "الفئات العمرية",
        ["Junior"] = "الناشئون",
        ["Open"] = "مفتوح",
        ["Youth"] = "الشباب",
        ["Masters"] = "الماسترز",
        ["Family"] = "العائلة",
        ["Relay"] = "التتابع",
        ["Para"] = "ذوو الإعاقة",
        ["U13"] = "تحت 13",
        ["U15"] = "تحت 15",
        ["U19"] = "تحت 19",
    };

    /// <summary>The bilingual label for a stored category value; unknown values come back unchanged in both.</summary>
    public static (string En, string Ar) Label(string category) =>
        (category, ArabicLabels.TryGetValue(category, out var ar) ? ar : category);
}
