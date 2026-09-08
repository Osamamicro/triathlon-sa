using System.Globalization;
using System.Text;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Services;

/// <summary>RFC 5545 output for the public calendar feed. Hand-written: the feed is one VEVENT per event with no recurrence.</summary>
public static class IcsWriter
{
    /// <summary>
    /// How long a timed event's VEVENT is assumed to run, for the DTEND a subscribing calendar
    /// needs — the site itself never asks an organiser for an end time. An all-day event does not use
    /// this: its DTEND is the exclusive day after <c>DateEnd ?? DateStart</c>, as before.
    /// </summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(4);

    public static string Write(IEnumerable<Event> events, string culture, string baseUrl, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(baseUrl);

        var ar = culture == "ar";
        var sb = new StringBuilder();
        Line(sb, "BEGIN:VCALENDAR");
        Line(sb, "VERSION:2.0");
        Line(sb, "PRODID:-//Saudi Triathlon Federation//triathlon.sa//" + culture.ToUpperInvariant());
        Line(sb, "CALSCALE:GREGORIAN");
        Line(sb, "METHOD:PUBLISH");
        Line(sb, "X-WR-CALNAME:" + Escape(ar ? "تقويم الاتحاد السعودي للترايثلون" : "Saudi Triathlon Federation calendar"));

        // Every dated event is in Riyadh time, so the zone is named once for the whole calendar
        // rather than per VEVENT. Saudi Arabia is UTC+3 with no daylight saving, which is why this
        // definition has no RRULE and no DAYLIGHT component.
        Line(sb, "BEGIN:VTIMEZONE");
        Line(sb, "TZID:Asia/Riyadh");
        Line(sb, "BEGIN:STANDARD");
        Line(sb, "DTSTART:19700101T000000");
        Line(sb, "TZOFFSETFROM:+0300");
        Line(sb, "TZOFFSETTO:+0300");
        Line(sb, "TZNAME:+03");
        Line(sb, "END:STANDARD");
        Line(sb, "END:VTIMEZONE");

        foreach (var e in events)
        {
            Line(sb, "BEGIN:VEVENT");
            Line(sb, "UID:" + e.Slug + "@triathlon.sa");
            Line(sb, "DTSTAMP:" + now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture));
            if (e.StartTime is { } t)
            {
                Line(sb, "DTSTART;TZID=Asia/Riyadh:" + e.DateStart.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "T" + t.ToString("HHmmss", CultureInfo.InvariantCulture));

                // A start after 20:00 pushes the default 4-hour duration past midnight, which
                // TimeOnly.Add wraps back into the small hours rather than carrying a day — clamped
                // to the last second of the same calendar day instead of an end that reads before
                // its own start.
                var end = t.Add(DefaultDuration);
                if (end < t)
                {
                    end = new TimeOnly(23, 59, 59);
                }

                Line(sb, "DTEND;TZID=Asia/Riyadh:" + (e.DateEnd ?? e.DateStart).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "T" + end.ToString("HHmmss", CultureInfo.InvariantCulture));
            }
            else
            {
                // A date-valued DTEND is exclusive, so a single-day event ends on the next morning.
                Line(sb, "DTSTART;VALUE=DATE:" + e.DateStart.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                Line(sb, "DTEND;VALUE=DATE:" + (e.DateEnd ?? e.DateStart).AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            }

            Line(sb, "SUMMARY:" + Escape(ar ? e.TitleAr : e.TitleEn));
            Line(sb, "DESCRIPTION:" + Escape(ar ? e.DescriptionAr : e.DescriptionEn));
            Line(sb, "LOCATION:" + Escape((ar ? e.VenueAr : e.VenueEn) + ", " + (ar ? e.City.NameAr : e.City.NameEn)));
            Line(sb, "URL:" + baseUrl.TrimEnd('/') + "/" + culture + "/events/" + e.Slug);
            Line(sb, "END:VEVENT");
        }

        Line(sb, "END:VCALENDAR");
        return sb.ToString();
    }

    /// <summary>
    /// The four characters RFC 5545 gives meaning to inside a TEXT value. The backslash goes first
    /// so the escapes the later replacements add are not escaped a second time.
    /// </summary>
    private static string Escape(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace(";", "\\;", StringComparison.Ordinal)
         .Replace(",", "\\,", StringComparison.Ordinal)
         .Replace("\r\n", "\\n", StringComparison.Ordinal)
         .Replace("\n", "\\n", StringComparison.Ordinal);

    /// <summary>Folds at 75 octets as the RFC requires; continuation lines start with one space.</summary>
    private static void Line(StringBuilder sb, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var start = 0;
        var first = true;
        while (start < bytes.Length)
        {
            var max = first ? 75 : 74;
            var len = Math.Min(max, bytes.Length - start);

            // Never split a multi-byte sequence: back off while the next octet is a continuation
            // byte (10xxxxxx), which would leave the pair either side of the fold unreadable.
            while (len > 0 && start + len < bytes.Length && (bytes[start + len] & 0xC0) == 0x80)
            {
                len--;
            }

            if (!first)
            {
                sb.Append(' ');
            }

            sb.Append(Encoding.UTF8.GetString(bytes, start, len)).Append("\r\n");
            start += len;
            first = false;
        }

        if (bytes.Length == 0)
        {
            sb.Append("\r\n");
        }
    }
}
