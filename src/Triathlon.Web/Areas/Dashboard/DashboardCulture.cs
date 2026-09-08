using System.Globalization;

namespace Triathlon.Web.Areas.Dashboard;

/// <summary>
/// The one culture check every dashboard screen that renders a bilingual value needs: which of the
/// two language-suffixed columns (<c>*En</c>/<c>*Ar</c>) to show for the editor's current UI
/// culture. Extracted from the copy that used to live separately on <c>Events/Index.razor</c> and
/// <c>Events/Edit.razor</c> (see F6, Week 4 QA report) so every screen — including Guides' Level
/// and Governance's Kind list columns, which never picked it up at all — agrees on the same rule.
/// </summary>
public static class DashboardCulture
{
    public static bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
}
