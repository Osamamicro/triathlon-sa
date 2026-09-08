using System.Text.Json;
using System.Text.Json.Serialization;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Services;

/// <summary>Records what a dashboard user did, for the audit trail shown in the admin panel.</summary>
public interface IActivityLogger
{
    /// <param name="entity">Entity type, e.g. <c>Event</c>.</param>
    /// <param name="entityId">Key of the row touched.</param>
    /// <param name="action">What happened, e.g. <c>update</c>.</param>
    /// <param name="before">State before the change, if any; serialised into the diff.</param>
    /// <param name="after">State after the change, if any; serialised into the diff.</param>
    Task LogAsync(
        string entity,
        string entityId,
        string action,
        object? before = null,
        object? after = null,
        CancellationToken ct = default);
}

/// <inheritdoc cref="IActivityLogger"/>
public sealed class ActivityLogger(
    AppDbContext db,
    TimeProvider timeProvider,
    ICurrentUser? currentUser = null) : IActivityLogger
{
    /// <summary>The user recorded when nobody is signed in behind the change — jobs, seeding, tests.</summary>
    public const string SystemUser = "system";

    private static readonly JsonSerializerOptions DiffOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Web defaults camel-case POCO property names but leave a Dictionary's own keys alone; a
        // before/after snapshot from Audit.Snapshot is a Dictionary<string, object?> keyed by the
        // entity's (Pascal-cased) reflection property names, so the policy needs saying twice for
        // the diff to read like the rest of the API instead of mixing "titleEn" and "TitleEn".
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task LogAsync(
        string entity,
        string entityId,
        string action,
        object? before = null,
        object? after = null,
        CancellationToken ct = default)
    {
        // Through ICurrentUser rather than the HTTP context: a dashboard edit is saved from a Blazor
        // circuit, which has no request behind it and would otherwise be logged as the system.
        var user = currentUser is null ? null : await currentUser.GetNameAsync(ct);

        db.ActivityLogs.Add(new ActivityLog
        {
            User = user ?? SystemUser,
            Entity = entity,
            EntityId = entityId,
            Action = action,
            At = timeProvider.GetUtcNow(),
            Diff = Diff(before, after),
        });

        await db.SaveChangesAsync(ct);
    }

    private static string? Diff(object? before, object? after) =>
        before is null && after is null ? null : JsonSerializer.Serialize(new Change(before, after), DiffOptions);

    /// <summary>The shape stored in <see cref="ActivityLog.Diff"/>: <c>{ "before": …, "after": … }</c>.</summary>
    private sealed record Change(object? Before, object? After);
}
