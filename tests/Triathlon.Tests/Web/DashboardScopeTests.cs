using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Areas.Dashboard;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// <see cref="DashboardScope"/> runs every dashboard save in its own DI scope so a refused save
/// never leaves a dirty <c>AppDbContext</c> behind for the circuit's next attempt. That scope still
/// has to resolve the acting user for the activity log: this covers the HTTP-request half of
/// <see cref="ICurrentUser"/> (<see cref="IHttpContextAccessor"/>) — the half a plain integration
/// test can reach. <see cref="ICurrentUser"/>'s other half, the Blazor circuit's own authentication
/// state, is not reachable over an <c>HttpClient</c> the way a real dashboard sign-in and edit is
/// (no SignalR circuit exists in this test host) and was verified manually instead — see the
/// Task 3.2.A report.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class DashboardScopeTests(WebAppFixture app)
{
    [Fact]
    public async Task RunAsync_resolves_ICurrentUser_to_the_request_principal_in_its_own_scope()
    {
        var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        try
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "scope-test@triathlon.test")], "Test");
            accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

            var scope = new DashboardScope(app.Services.GetRequiredService<IServiceScopeFactory>());
            var name = await scope.RunAsync(sp => Task.FromResult(sp.GetRequiredService<ICurrentUser>().Name));

            Assert.Equal("scope-test@triathlon.test", name);
        }
        finally
        {
            accessor.HttpContext = previous;
        }
    }
}
