using Bunit;
using Microsoft.AspNetCore.Components;
using Triathlon.Web.Areas.Dashboard.Components;

namespace Triathlon.Tests.Components;

/// <summary>Every content editor's save/publish bar, so its button wiring is covered once instead of per screen.</summary>
public sealed class PublishBarTests : BunitContext
{
    [Fact]
    public void Draft_state_shows_the_Draft_chip_and_a_Publish_button()
    {
        var cut = Render<PublishBar>(parameters => parameters.Add(p => p.IsPublished, false));

        Assert.Contains("Draft", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Publish", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Clicking_Publish_invokes_OnPublish()
    {
        var invoked = false;
        var cut = Render<PublishBar>(parameters => parameters
            .Add(p => p.IsPublished, false)
            .Add(p => p.OnPublish, EventCallback.Factory.Create(this, () => invoked = true)));

        var publishButton = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Publish");
        publishButton.Click();

        Assert.True(invoked);
    }

    [Fact]
    public void Saving_disables_the_publish_button()
    {
        var cut = Render<PublishBar>(parameters => parameters
            .Add(p => p.IsPublished, false)
            .Add(p => p.IsSaving, true));

        var publishButton = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Publish");

        Assert.True(publishButton.HasAttribute("disabled"));
    }
}
