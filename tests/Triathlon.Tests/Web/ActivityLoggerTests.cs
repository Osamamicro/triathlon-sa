using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Data;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// The activity log is the audit trail behind every dashboard edit, so what it stores has to be a
/// readable JSON diff rather than a stringified object graph.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class ActivityLoggerTests(WebAppFixture app)
{
    [Fact]
    public async Task Activity_logger_writes_json_diff()
    {
        var entityId = Guid.CreateVersion7().ToString();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var activity = scope.ServiceProvider.GetRequiredService<IActivityLogger>();
            await activity.LogAsync(
                "Event",
                entityId,
                "update",
                before: new { Name = "Jeddah Sprint" },
                after: new { Name = "Jeddah Sprint 2026" });
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.ActivityLogs.SingleAsync(log => log.EntityId == entityId);

            // No HTTP context in a background scope, so the author is the system.
            Assert.Equal("system", row.User);
            Assert.Equal("Event", row.Entity);
            Assert.Equal("update", row.Action);
            Assert.NotEqual(default, row.At);

            using var diff = JsonDocument.Parse(row.Diff!);
            Assert.True(diff.RootElement.TryGetProperty("before", out var before));
            Assert.True(diff.RootElement.TryGetProperty("after", out var after));
            Assert.Equal("Jeddah Sprint", before.GetProperty("name").GetString());
            Assert.Equal("Jeddah Sprint 2026", after.GetProperty("name").GetString());
        }
    }
}
