using System.Globalization;
using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Areas.Public;

/// <summary>
/// Single-culture text for the public views. Request localization sets the culture from the URL
/// segment, so a view that calls <c>Bi(en, ar)</c> emits one language and nothing for the other —
/// the paired <c>.en/.ar</c> spans of the prototype are gone from server markup (Week 1 ruling).
/// Dates are Gregorian in both languages; Arabic shows Arabic-Indic digits, as the prototype did.
/// </summary>
public static class PublicText
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");
    // Neutral "ar", not "ar-SA": ar-SA defaults to the Umm al-Qura calendar.
    private static readonly CultureInfo Arabic = CultureInfo.GetCultureInfo("ar");

    public static bool IsArabic =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase);

    public static string Culture => IsArabic ? "ar" : "en";

    public static string Bi(string en, string ar) => IsArabic ? ar : en;

    public static string Bi(Bilingual text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return Bi(text.En, text.Ar);
    }

    /// <summary>Arabic-Indic digits for Arabic; every other character, and every other culture, untouched.</summary>
    public static string Digits(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!IsArabic)
        {
            return text;
        }

        return string.Create(text.Length, text, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = c is >= '0' and <= '9' ? (char)('\u0660' + (c - '0')) : c;
            }
        });
    }

    /// <summary>
    /// Thousands are grouped with the Arabic thousands separator (U+066C) rather than a comma,
    /// which is what the digits belong with; the grouping itself comes from the English format
    /// because Arabic's own numeric formats vary by region and this site shows one shape.
    /// </summary>
    public static string Number(long value) =>
        IsArabic ? Digits(value.ToString("N0", English)).Replace(',', '\u066C') : value.ToString("N0", English);

    public static string LongDate(DateOnly date) =>
        IsArabic ? Digits(date.ToString("dddd d MMMM yyyy", Arabic)) : date.ToString("ddd d MMMM yyyy", English);

    public static string DayNumber(DateOnly date) => Digits(date.Day.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Short in English ("Oct"); Arabic has no shortened month names worth the ambiguity, so it
    /// gets the full one.
    /// </summary>
    public static string MonthShort(DateOnly date) =>
        IsArabic ? date.ToString("MMMM", Arabic) : date.ToString("MMM", English);

    public static string MonthYear(DateOnly date) =>
        Digits(date.ToString("MMMM yyyy", IsArabic ? Arabic : English));

    public static string Time(TimeOnly time) => Digits(time.ToString("HH:mm", CultureInfo.InvariantCulture));

    /// <summary>
    /// Whether a value edited through the dashboard is safe to render into an <c>href</c>: an
    /// absolute <c>http</c>/<c>https</c> URL. Blocks <c>javascript:</c>, <c>data:</c> and malformed
    /// values, and a relative path (which would resolve against the current page rather than go
    /// where the editor intended). Callers fall back to a disabled placeholder when this is false.
    /// </summary>
    public static bool IsSafeExternalUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url, UriKind.Absolute, out var parsed)
        && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
}
