using System.Globalization;
using Triathlon.Web.Areas.Public;

namespace Triathlon.Tests.Areas;

public sealed class PublicTextTests
{
    private static T Under<T>(string culture, Func<T> read)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try { return read(); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void Bi_picks_the_current_culture()
    {
        Assert.Equal("Events", Under("en", () => PublicText.Bi("Events", "الفعاليات")));
        Assert.Equal("الفعاليات", Under("ar", () => PublicText.Bi("Events", "الفعاليات")));
        // The route constraint is case-insensitive, so "AR" must still read as Arabic.
        Assert.Equal("الفعاليات", Under("AR", () => PublicText.Bi("Events", "الفعاليات")));
    }

    [Fact]
    public void Dates_are_gregorian_in_both_languages_with_arabic_digits_in_arabic()
    {
        var date = new DateOnly(2026, 10, 17);
        Assert.Equal("Sat 17 October 2026", Under("en", () => PublicText.LongDate(date)));
        Assert.Equal("السبت ١٧ أكتوبر ٢٠٢٦", Under("ar", () => PublicText.LongDate(date)));
        Assert.Equal("Oct", Under("en", () => PublicText.MonthShort(date)));
        Assert.Equal("أكتوبر", Under("ar", () => PublicText.MonthShort(date)));
        Assert.Equal("١٧", Under("ar", () => PublicText.DayNumber(date)));
    }

    [Fact]
    public void Numbers_group_thousands_per_culture()
    {
        Assert.Equal("18,650", Under("en", () => PublicText.Number(18650)));
        Assert.Equal("١٨٬٦٥٠", Under("ar", () => PublicText.Number(18650)));
        Assert.Equal("٠٦:٣٠", Under("ar", () => PublicText.Time(new TimeOnly(6, 30))));
    }
}
