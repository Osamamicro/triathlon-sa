using Bunit;
using Triathlon.Web.Areas.Dashboard.Components;

namespace Triathlon.Tests.Components;

/// <summary>
/// Every bilingual dashboard form leans on <see cref="BilingualField"/> for its English/Arabic pairs,
/// so its required-side validation and RTL markup are worth locking down on their own, not just
/// through whichever screen happens to use it first.
/// </summary>
public sealed class BilingualFieldTests : BunitContext
{
    [Fact]
    public void Blank_required_Arabic_side_shows_no_error_until_it_is_touched()
    {
        var cut = Render<BilingualField>(parameters => parameters
            .Add(p => p.Label, "Title")
            .Add(p => p.Required, true)
            .Add(p => p.En, "Hello")
            .Add(p => p.Ar, ""));

        // IsValid does not wait for a blur — a caller checks it right before saving, whether or not
        // the user ever focused the field.
        Assert.False(cut.Instance.IsValid);
        Assert.DoesNotContain("Required", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Blank_required_Arabic_side_shows_Required_once_touched()
    {
        var cut = Render<BilingualField>(parameters => parameters
            .Add(p => p.Label, "Title")
            .Add(p => p.Required, true)
            .Add(p => p.En, "Hello")
            .Add(p => p.Ar, ""));

        cut.Find("input[dir='rtl']").Blur();

        Assert.Contains("Required", cut.Markup, StringComparison.Ordinal);
        Assert.False(cut.Instance.IsValid);
    }

    [Fact]
    public void Filled_Arabic_side_has_no_error_and_is_valid()
    {
        var cut = Render<BilingualField>(parameters => parameters
            .Add(p => p.Label, "Title")
            .Add(p => p.Required, true)
            .Add(p => p.En, "Hello")
            .Add(p => p.Ar, "مرحبا"));

        cut.Find("input[dir='rtl']").Blur();

        Assert.DoesNotContain("Required", cut.Markup, StringComparison.Ordinal);
        Assert.True(cut.Instance.IsValid);
    }

    [Fact]
    public void The_Arabic_input_carries_dir_rtl()
    {
        var cut = Render<BilingualField>(parameters => parameters
            .Add(p => p.Label, "Title")
            .Add(p => p.Required, true)
            .Add(p => p.En, "Hello")
            .Add(p => p.Ar, "مرحبا"));

        var arabicInput = cut.Find("input[dir='rtl']");

        Assert.Equal("rtl", arabicInput.GetAttribute("dir"));
    }
}
