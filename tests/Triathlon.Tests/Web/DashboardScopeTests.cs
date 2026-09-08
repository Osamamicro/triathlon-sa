using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Areas.Dashboard;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// <see cref="DashboardScope"/> runs every dashboard save in its own DI scope so a refused save
/// never leaves a dirty <c>AppDbContext</c> behind for the circuit's next attempt. That fresh child
/// scope has its own, unrelated <c>AuthenticationStateProvider</c> with no state ever set on it, so
/// without carrying the acting user across explicitly, the child scope's <see cref="ICurrentUser"/>
/// could not see who is signed in on the circuit that opened it. These tests cover both of
/// <see cref="ICurrentUser"/>'s paths from the caller's side, proving the name still reaches the
/// child scope through <see cref="ActingUser"/> either way.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class DashboardScopeTests(WebAppFixture app)
{
    /// <summary>
    /// A stand-in for the real <c>IdentityRevalidatingAuthenticationStateProvider</c>: registered
    /// scoped, so — exactly like production — every DI scope gets its own instance with no principal
    /// set until something sets one. Only the parent scope's instance below ever has one; the child
    /// scope <see cref="DashboardScope"/> creates gets a fresh instance whose principal is still
    /// null, so if the acting user reached the child scope by any means other than
    /// <see cref="ActingUser"/>, this test would not be exercising that path.
    /// </summary>
    private sealed class FakeCircuitAuthStateProvider : AuthenticationStateProvider
    {
        public ClaimsPrincipal? Principal { get; set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Principal is null
                ? throw new InvalidOperationException("No authentication state has been set for this circuit scope.")
                : Task.FromResult(new AuthenticationState(Principal));
    }

    [Fact]
    public async Task RunAsync_carries_the_circuits_authenticated_user_into_the_child_scope()
    {
        var services = new ServiceCollection();
        services.AddScoped<AuthenticationStateProvider, FakeCircuitAuthStateProvider>();
        services.AddScoped<ActingUser>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<DashboardScope>();
        await using var provider = services.BuildServiceProvider();

        // The "circuit": a parent scope whose own AuthenticationStateProvider instance is given an
        // authenticated principal, and which has no IHttpContextAccessor at all (a circuit does not
        // reliably have one either — see ActivityLogger.cs).
        await using var circuit = provider.CreateAsyncScope();
        var circuitAuthState = (FakeCircuitAuthStateProvider)circuit.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
        circuitAuthState.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "editor@triathlon.sa")], "Test"));

        var scope = circuit.ServiceProvider.GetRequiredService<DashboardScope>();

        var (syncName, asyncName) = await scope.RunAsync(async sp =>
        {
            var user = sp.GetRequiredService<ICurrentUser>();
            return (Sync: user.Name, Async: await user.GetNameAsync());
        });

        Assert.Equal("editor@triathlon.sa", syncName);
        Assert.Equal("editor@triathlon.sa", asyncName);
    }

    /// <summary>
    /// The fallback half: when the caller-side <see cref="ICurrentUser"/> has no circuit
    /// authentication state to answer from (the real app's provider throws until a circuit sets
    /// one), it falls back to <see cref="IHttpContextAccessor"/> — and that resolved name still has
    /// to reach the child scope through <see cref="ActingUser"/> the same way the circuit path does.
    /// Uses the real app host (rather than a hand-built container) so the real
    /// <c>IdentityRevalidatingAuthenticationStateProvider</c>'s "no state set" behaviour is what is
    /// actually being exercised here, not a stand-in for it.
    /// </summary>
    [Fact]
    public async Task RunAsync_falls_back_to_the_HTTP_request_principal_when_the_circuit_has_none()
    {
        var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        try
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "scope-test@triathlon.test")], "Test");
            accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

            await using var callerScope = app.Services.CreateAsyncScope();
            var circuitUser = callerScope.ServiceProvider.GetRequiredService<ICurrentUser>();
            var scope = new DashboardScope(app.Services.GetRequiredService<IServiceScopeFactory>(), circuitUser);

            var name = await scope.RunAsync(sp => Task.FromResult(sp.GetRequiredService<ICurrentUser>().Name));

            Assert.Equal("scope-test@triathlon.test", name);
        }
        finally
        {
            accessor.HttpContext = previous;
        }
    }
}
