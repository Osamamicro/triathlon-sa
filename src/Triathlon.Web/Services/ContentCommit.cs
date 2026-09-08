using Microsoft.AspNetCore.OutputCaching;

namespace Triathlon.Web.Services;

/// <summary>
/// The tail of every dashboard mutation: one activity-log row (whose save is the unit of work's
/// only <c>SaveChanges</c>), then the cache tags the change touched. Services mutate tracked
/// entities and call this once; nothing else on a dashboard path saves the context.
/// </summary>
public sealed class ContentCommit(IActivityLogger activity, IOutputCacheStore cache)
{
    public async Task ApplyAsync(string entity, Guid id, string action, object? before, object? after, IReadOnlyCollection<string> tags, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tags);
        await activity.LogAsync(entity, id.ToString(), action, before, after, ct);
        await cache.EvictAsync(ct, [.. tags]);
    }
}
