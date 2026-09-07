using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Triathlon.Web.Services;

/// <inheritdoc cref="ICurrentUser"/>
/// <remarks>
/// Scoped, because both sources it reads are: a Blazor circuit's authentication state lives in the
/// circuit's scope, and <see cref="IHttpContextAccessor"/> answers for the scope's request.
/// </remarks>
public sealed class CurrentUser(IServiceProvider services, IHttpContextAccessor? httpContextAccessor = null)
    : ICurrentUser
{
    public string? Name
    {
        get
        {
            // Already-completed is the normal case: ServerAuthenticationStateProvider hands back the
            // state the circuit was opened with. If it is genuinely pending, this synchronous caller
            // cannot wait for it, so fall through rather than block a save on a network round trip.
            var state = AuthenticationState();

            return (state is { IsCompletedSuccessfully: true } ? NameOf(state.Result.User) : null)
                ?? HttpContextName();
        }
    }

    public async ValueTask<string?> GetNameAsync(CancellationToken ct = default)
    {
        var state = AuthenticationState();

        if (state is not null)
        {
            var name = NameOf((await state.WaitAsync(ct).ConfigureAwait(false)).User);
            if (name is not null)
            {
                return name;
            }
        }

        return HttpContextName();
    }

    /// <summary>
    /// The circuit's authentication state, or <c>null</c> when this scope has none.
    /// <para>
    /// Resolved on demand rather than injected: the dashboard's provider revalidates through
    /// Identity, which resolves the same <c>DbContext</c> this service is constructed alongside, so
    /// taking it as a constructor dependency would close a resolution loop. Outside a circuit the
    /// provider is registered but never had a state set, and answering that is an
    /// <see cref="InvalidOperationException"/> rather than an anonymous principal.
    /// </para>
    /// </summary>
    private Task<AuthenticationState>? AuthenticationState()
    {
        var provider = services.GetService<AuthenticationStateProvider>();
        if (provider is null)
        {
            return null;
        }

        try
        {
            return provider.GetAuthenticationStateAsync();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private string? HttpContextName() => NameOf(httpContextAccessor?.HttpContext?.User);

    private static string? NameOf(ClaimsPrincipal? principal) =>
        principal?.Identity is { IsAuthenticated: true, Name: { Length: > 0 } name } ? name : null;
}
