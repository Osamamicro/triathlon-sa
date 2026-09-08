using Triathlon.Web.Domain.Events;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

public sealed class IcsWriterTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 6, 0, 0, TimeSpan.Zero);

    private static Event Sample(string slug, string title, DateOnly date, TimeOnly? time) => new()
    {
        Slug = slug, Type = EventType.Competition, IsPublished = true, Season = "2026-27", DateStart = date, StartTime = time,
        City = new City { Key = "riyadh", NameEn = "Riyadh", NameAr = "الرياض" },
        TitleEn = title, TitleAr = "عنوان", VenueEn = "King Salman Park", VenueAr = "حديقة الملك سلمان", DescriptionEn = "Desc, with comma; and semicolon", DescriptionAr = "وصف",
    };

    [Fact]
    public void One_vevent_per_event_with_riyadh_timezone_and_escaped_text()
    {
        var ics = IcsWriter.Write(
        [
            Sample("a", "Riyadh Sprint", new DateOnly(2026, 10, 17), new TimeOnly(6, 0)),
            Sample("b", "All-day", new DateOnly(2026, 11, 21), null),
        ], "en", "https://triathlon.sa", Now);

        Assert.Equal(2, ics.Split("BEGIN:VEVENT").Length - 1);
        Assert.Contains("DTSTAMP:20260907T060000Z", ics, StringComparison.Ordinal);
        Assert.Contains("DTSTART;TZID=Asia/Riyadh:20261017T060000", ics, StringComparison.Ordinal);
        // The default 4-hour duration: 06:00 + 4:00 = 10:00, same calendar day.
        Assert.Contains("DTEND;TZID=Asia/Riyadh:20261017T100000", ics, StringComparison.Ordinal);
        Assert.Contains("DTSTART;VALUE=DATE:20261121", ics, StringComparison.Ordinal);
        Assert.Contains("DTEND;VALUE=DATE:20261122", ics, StringComparison.Ordinal);
        Assert.Contains("UID:a@triathlon.sa", ics, StringComparison.Ordinal);
        Assert.Contains("URL:https://triathlon.sa/en/events/a", ics, StringComparison.Ordinal);
        Assert.Contains("DESCRIPTION:Desc\\, with comma\\; and semicolon", ics, StringComparison.Ordinal);
        Assert.Contains("LOCATION:King Salman Park\\, Riyadh", ics, StringComparison.Ordinal);
        Assert.EndsWith("END:VCALENDAR\r\n", ics, StringComparison.Ordinal);
        Assert.All(ics.Split("\r\n"), line => Assert.True(line.Length <= 75, "line not folded: " + line));
    }

    [Fact]
    public void A_late_start_clamps_the_default_duration_to_the_same_calendar_day()
    {
        // 22:00 + 4:00 would wrap past midnight; TimeOnly.Add wraps rather than carries a day, so
        // the writer clamps to the last second of the same day instead of an end before its start.
        var ics = IcsWriter.Write(
            [Sample("late", "Late Start", new DateOnly(2026, 10, 17), new TimeOnly(22, 0))],
            "en", "https://triathlon.sa", Now);

        Assert.Contains("DTEND;TZID=Asia/Riyadh:20261017T235959", ics, StringComparison.Ordinal);
    }

    [Fact]
    public void Arabic_text_is_folded_without_splitting_a_character()
    {
        // A long Arabic title is the only place the 75-octet fold actually bites, and UTF-8 makes
        // every one of those characters two octets — folding by character count would corrupt it.
        var title = string.Join(' ', Enumerable.Repeat("بطولة", 20));
        var ev = Sample("long", "Long", new DateOnly(2026, 10, 17), new TimeOnly(6, 0));
        ev.TitleAr = title;

        var ics = IcsWriter.Write([ev], "ar", "https://triathlon.sa", Now);
        var unfolded = ics.Replace("\r\n ", "", StringComparison.Ordinal);

        Assert.Contains("SUMMARY:" + title, unfolded, StringComparison.Ordinal);
        Assert.All(ics.Split("\r\n"), line =>
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(line) <= 75, "line not folded: " + line));
    }
}
