using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// Every audit stamp and every activity-log row names whoever this service says is acting, and the
/// dashboard saves from a Blazor circuit where there is no <c>HttpContext</c> to ask. So both
/// sources are pinned down here, along with the order between them.
/// </summary>
public sealed class CurrentUserTests
{
    private const string RequestUser = "request@triathlon.test";
    private const string CircuitUser = "circuit@triathlon.test";

    [Fact]
    public async Task Reads_the_request_principal_when_there_is_no_circuit()
    {
        var currentUser = Resolve(accessor: Accessor(RequestUser));

        Assert.Equal(RequestUser, currentUser.Name);
        Assert.Equal(RequestUser, await currentUser.GetNameAsync());
    }

    [Fact]
    public async Task Reads_the_circuit_authentication_state_when_one_is_registered()
    {
        var currentUser = Resolve(authenticationState: new FakeAuthenticationStateProvider(CircuitUser));

        Assert.Equal(CircuitUser, currentUser.Name);
        Assert.Equal(CircuitUser, await currentUser.GetNameAsync());
    }

    [Fact]
    public async Task The_circuit_wins_over_the_request()
    {
        // Both exist only in the odd case of a circuit's scope also carrying a request; the circuit
        // is the one that started the save.
        var currentUser = Resolve(Accessor(RequestUser), new FakeAuthenticationStateProvider(CircuitUser));

        Assert.Equal(CircuitUser, currentUser.Name);
        Assert.Equal(CircuitUser, await currentUser.GetNameAsync());
    }

    [Fact]
    public async Task Nobody_signed_in_is_null_rather_than_a_name()
    {
        var currentUser = Resolve();

        Assert.Null(currentUser.Name);
        Assert.Null(await currentUser.GetNameAsync());
    }

    [Fact]
    public async Task An_anonymous_request_principal_is_not_a_name()
    {
        // ClaimsIdentity with no authentication type: signed out, not signed in as "".
        var currentUser = Resolve(accessor: new TestHttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) },
        });

        Assert.Null(currentUser.Name);
        Assert.Null(await currentUser.GetNameAsync());
    }

    [Fact]
    public async Task A_circuit_that_never_had_its_state_set_falls_through_to_the_request()
    {
        // What the real provider does outside a circuit: GetAuthenticationStateAsync throws rather
        // than answering "anonymous", and that must not cost the request its user.
        var currentUser = Resolve(Accessor(RequestUser), new UnsetAuthenticationStateProvider());

        Assert.Equal(RequestUser, currentUser.Name);
        Assert.Equal(RequestUser, await currentUser.GetNameAsync());
    }

    /// <summary>Builds the service exactly as the application does — resolved out of a scope.</summary>
    private static ICurrentUser Resolve(
        IHttpContextAccessor? accessor = null,
        AuthenticationStateProvider? authenticationState = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<ActingUser>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        if (accessor is not null)
        {
            services.AddSingleton(accessor);
        }

        if (authenticationState is not null)
        {
            services.AddScoped(_ => authenticationState);
        }

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        return scope.ServiceProvider.GetRequiredService<ICurrentUser>();
    }

    private static TestHttpContextAccessor Accessor(string name) => new()
    {
        HttpContext = new DefaultHttpContext { User = Principal(name) },
    };

    private static ClaimsPrincipal Principal(string name) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], authenticationType: "test"));

    /// <summary>A plain holder, so nothing here depends on the real accessor's ambient state.</summary>
    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class FakeAuthenticationStateProvider(string name) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(Principal(name)));
    }

    private sealed class UnsetAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            throw new InvalidOperationException(
                "GetAuthenticationStateAsync was called before SetAuthenticationState.");
    }
}
