using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Dashboard;

/// <summary>
/// Runs one unit of work — a read-for-edit or a save — in its own DI scope, so a refused save
/// (a <c>ContentValidationException</c>) never leaves a dirty <c>AppDbContext</c> parked in the
/// Blazor circuit's own scope for the next attempt on the same page to stumble into. Every
/// dashboard content page resolves its write services through this instead of taking them as a
/// page-level <c>@inject</c>.
/// <para>
/// The child scope this creates is unrelated to the circuit's own scope — it has its own, freshly
/// constructed <c>AuthenticationStateProvider</c> with no state ever set on it — so left alone its
/// <see cref="ICurrentUser"/> could not see who is signed in, and every save would be attributed to
/// nobody. <paramref name="circuitUser"/> is the <em>caller's</em> (the circuit's or request's)
/// <see cref="ICurrentUser"/>, resolved once per call before the child scope exists; its name is
/// stamped onto the child scope's <see cref="ActingUser"/> marker so the child scope's own
/// <see cref="ICurrentUser"/> answers with the same acting user instead of losing them.
/// </para>
/// </summary>
public sealed class DashboardScope(IServiceScopeFactory scopes, ICurrentUser circuitUser)
{
    public async Task<T> RunAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        var actingName = await circuitUser.GetNameAsync().ConfigureAwait(false);
        await using var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ActingUser>().Name = actingName;
        return await work(scope.ServiceProvider);
    }

    public async Task RunAsync(Func<IServiceProvider, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        var actingName = await circuitUser.GetNameAsync().ConfigureAwait(false);
        await using var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ActingUser>().Name = actingName;
        await work(scope.ServiceProvider);
    }
}
