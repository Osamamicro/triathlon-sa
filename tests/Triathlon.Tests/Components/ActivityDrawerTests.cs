using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Areas.Dashboard.Components;
using Triathlon.Web.Domain.Identity;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Components;

/// <summary>
/// Covers the fix round 1 finding: <c>@bind-Open="Open"</c> on the drawer's own <c>Open</c>
/// parameter binds MudDrawer's parameter to that field and stops there — it never calls the
/// component's own <see cref="ActivityDrawer.OpenChanged"/>, so a parent's own <c>@bind-Open</c>
/// on &lt;ActivityDrawer&gt; was dead and the drawer could not be closed from the scrim. Registers
/// a stub <see cref="IActivityQuery"/> instead of a real <see cref="Triathlon.Web.Data.AppDbContext"/>-backed one.
/// </summary>
public sealed class ActivityDrawerTests : BunitContext
{
    public ActivityDrawerTests()
    {
        Services.AddScoped<IActivityQuery>(_ => new EmptyActivityQuery());
    }

    [Fact]
    public async Task Closing_the_drawer_invokes_the_parents_OpenChanged_with_false()
    {
        bool? received = null;
        var cut = Render<ActivityDrawer>(parameters => parameters
            .Add(p => p.Entity, "Event")
            .Add(p => p.Open, true)
            .Add(p => p.OpenChanged, EventCallback.Factory.Create<bool>(this, value => received = value)));

        var drawer = cut.FindComponent<MudBlazor.MudDrawer>();
        await cut.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(false));

        Assert.Equal(false, received);
    }

    private sealed class EmptyActivityQuery : IActivityQuery
    {
        public Task<IReadOnlyList<ActivityLog>> ForEntityAsync(string entity, Guid? id, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ActivityLog>>([]);

        public Task<IReadOnlyList<ActivityLog>> LatestAsync(int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ActivityLog>>([]);
    }
}
