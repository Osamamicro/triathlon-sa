using Microsoft.Extensions.Options;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

public sealed class ContentGuardTests
{
    private static ContentGuard Guard(string prefix = "/media") =>
        new(Options.Create(new MediaOptions { PublicPrefix = prefix }));

    [Fact]
    public void Scripts_handlers_and_javascript_urls_are_stripped_but_formatting_survives()
    {
        var dirty = "<p class=\"muted\" onclick=\"x()\">Hi <strong>there</strong><script>alert(1)</script>" +
                    "<a href=\"javascript:alert(1)\">bad</a> <a href=\"https://triathlon.sa\" target=\"_blank\">ok</a>" +
                    "<img src=\"x\" onerror=\"alert(1)\"><span style=\"color:red\">styled</span></p>";

        var clean = Guard().Html(dirty)!;

        Assert.DoesNotContain("<script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p class=\"muted\">", clean, StringComparison.Ordinal);
        Assert.Contains("<strong>there</strong>", clean, StringComparison.Ordinal);
        Assert.Contains("href=\"https://triathlon.sa\"", clean, StringComparison.Ordinal);
        Assert.Contains("<span>styled</span>", clean, StringComparison.Ordinal);
    }

    [Fact]
    public void Arabic_text_and_the_seeded_markup_shapes_pass_through_unchanged()
    {
        const string hero = "<span class=\"grad\">ترايثلون</span><br>السعودية";
        const string list = "<ul class=\"list-check\"><li>اربط الخوذة</li></ul>";

        Assert.Equal(hero, Guard().Html(hero));
        Assert.Equal(list, Guard().Html(list));
        Assert.Null(Guard().Html("   "));
        Assert.Null(Guard().Html(null));
    }

    [Theory]
    [InlineData("/docs/annual-report-2025.pdf", true)]
    [InlineData("/media/2026/09/abc.webp", true)]
    [InlineData("/uploads/2026/09/abc.webp", false)]
    [InlineData("/docs/../appsettings.json", false)]
    [InlineData("//evil.example/x.pdf", false)]
    [InlineData("https://evil.example/x.pdf", false)]
    [InlineData("/docs/x.pdf?y=1", false)]
    [InlineData("docs/x.pdf", false)]
    [InlineData("/docs/", false)]
    [InlineData("/docs/%2e%2e/appsettings.json", false)]
    [InlineData("/docs/x\r\ny.pdf", false)]
    [InlineData("/docs/x y.pdf", false)]
    public void Only_site_relative_docs_and_media_paths_are_accepted(string path, bool ok)
    {
        Assert.Equal(ok, Guard().IsSiteFilePath(path));
        if (ok) Assert.Equal(path, Guard().FilePath(path));
        else Assert.Throws<ContentValidationException>(() => Guard().FilePath(path));
    }

    [Fact]
    public void The_media_prefix_follows_configuration()
    {
        Assert.True(Guard("/files").IsSiteFilePath("/files/2026/09/a.pdf"));
        Assert.False(Guard("/files").IsSiteFilePath("/media/2026/09/a.pdf"));
    }
}
