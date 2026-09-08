using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace Triathlon.Tests.Web;

/// <summary>
/// Runs an enqueued job right now, on the caller's thread, so a test that posts a form can assert on
/// the mail it caused without waiting for the Hangfire server. Recurring jobs are untouched.
/// </summary>
public sealed class InlineJobClient(IServiceScopeFactory scopes) : IBackgroundJobClient
{
    public string Create(Job job, IState state)
    {
        ArgumentNullException.ThrowIfNull(job);
        using var scope = scopes.CreateScope();
        var instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, job.Type);
        var result = job.Method.Invoke(instance, [.. job.Args]);
        if (result is Task task)
        {
            task.GetAwaiter().GetResult();
        }
        return Guid.NewGuid().ToString("N");
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;
}
