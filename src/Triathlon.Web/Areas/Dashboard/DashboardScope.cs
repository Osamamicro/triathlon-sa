namespace Triathlon.Web.Areas.Dashboard;

/// <summary>
/// Runs one unit of work — a read-for-edit or a save — in its own DI scope, so a refused save
/// (a <c>ContentValidationException</c>) never leaves a dirty <c>AppDbContext</c> parked in the
/// Blazor circuit's own scope for the next attempt on the same page to stumble into. Every
/// dashboard content page resolves its write services through this instead of taking them as a
/// page-level <c>@inject</c>.
/// </summary>
public sealed class DashboardScope(IServiceScopeFactory scopes)
{
    public async Task<T> RunAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = scopes.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    public async Task RunAsync(Func<IServiceProvider, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = scopes.CreateAsyncScope();
        await work(scope.ServiceProvider);
    }
}
