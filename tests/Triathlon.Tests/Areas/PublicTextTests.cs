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

    [Fact]
    public void Editor_written_links_keep_absolute_targets_and_gain_the_culture_segment()
    {
        // Paths inside the site, including the home page and a path carrying a fragment.
        Assert.Equal("/en/events", Under("en", () => PublicText.Href("events")));
        Assert.Equal("/ar/join#clubs", Under("ar", () => PublicText.Href("join#clubs")));
        Assert.Equal("/en", Under("en", () => PublicText.Href("")));

        // Targets that already say where they go are left alone.
        Assert.Equal("https://x.com/triathlonksa", Under("en", () => PublicText.Href("https://x.com/triathlonksa")));
        Assert.Equal("http://example.test/a", Under("en", () => PublicText.Href("http://example.test/a")));
        Assert.Equal("mailto:info@triathlon.sa", Under("ar", () => PublicText.Href("mailto:info@triathlon.sa")));
        Assert.Equal("#clubs", Under("en", () => PublicText.Href("#clubs")));
    }

    [Fact]
    public void IsBlockHtml_recognises_block_level_tags_and_nothing_else()
    {
        Assert.True(PublicText.IsBlockHtml("<p>Some copy.</p>"));
        Assert.True(PublicText.IsBlockHtml("<ul class=\"list-check\"><li>One</li></ul>"));
        Assert.True(PublicText.IsBlockHtml("  \n  <div>Indented</div>"));
        Assert.False(PublicText.IsBlockHtml("Just a plain sentence."));
        Assert.False(PublicText.IsBlockHtml(null));
        Assert.False(PublicText.IsBlockHtml(""));
        Assert.False(PublicText.IsBlockHtml("   "));
    }

    [Fact]
    public void AccentVar_whitelists_the_three_discipline_colours()
    {
        Assert.Equal("var(--swim)", PublicText.AccentVar("swim"));
        Assert.Equal("var(--bike)", PublicText.AccentVar("bike"));
        Assert.Equal("var(--run)", PublicText.AccentVar("Run"));
        Assert.Null(PublicText.AccentVar("x);background:url(a)"));
        Assert.Null(PublicText.AccentVar(null));
        Assert.Null(PublicText.AccentVar(""));
    }
}
