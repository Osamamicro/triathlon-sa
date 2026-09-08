using Triathlon.Web.Areas.Public;
using Triathlon.Web.Domain.Common;

namespace Triathlon.Tests.Areas;

public sealed class SlugsTests
{
    [Theory]
    [InlineData("riyadh-sprint-2026", true)]
    [InlineData("a", true)]
    [InlineData("Riyadh", false)]
    [InlineData("has space", false)]
    [InlineData("dot.pdf", false)]
    [InlineData("-leading", false)]
    [InlineData("double--dash", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Slug_format_is_lower_case_words_joined_by_single_hyphens(string? slug, bool expected) =>
        Assert.Equal(expected, Slugs.IsValid(slug));

    [Fact]
    public void Length_is_capped_at_128_characters()
    {
        Assert.True(Slugs.IsValid(new string('a', 128)));
        Assert.False(Slugs.IsValid(new string('a', 129)));
    }

    [Fact]
    public void Reserved_slugs_cover_every_dedicated_public_route()
    {
        foreach (var route in new[] { "events", "register", "training", "rules", "governance", "statistics", "news", "not-found", "error", "api", "media", "docs", "dashboard", "health" })
        {
            Assert.Contains(route, PublicSite.ReservedSlugs);
        }

        Assert.DoesNotContain("join", PublicSite.ReservedSlugs);
        Assert.DoesNotContain("contact", PublicSite.ReservedSlugs);
        Assert.Contains("rules", PublicSite.CompanionPageSlugs);
        Assert.Contains("home", PublicSite.CompanionPageSlugs);
        Assert.Contains("training", PublicSite.CompanionPageSlugs);

        // governance and statistics stay reserved (they are still dedicated public routes above),
        // but neither is a companion slug: no public page renders a CMS page's blocks under either.
        Assert.DoesNotContain("governance", PublicSite.CompanionPageSlugs);
        Assert.DoesNotContain("statistics", PublicSite.CompanionPageSlugs);
    }
}
