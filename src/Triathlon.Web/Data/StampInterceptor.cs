using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Services;

namespace Triathlon.Web.Data;

/// <summary>
/// Fills the audit stamps on every <see cref="BaseEntity"/> and turns hard deletes into soft deletes.
/// <para>
/// The author comes from <see cref="ICurrentUser"/>, which answers for a Blazor circuit as well as an
/// HTTP request — the dashboard saves on a circuit, where there is no <c>HttpContext</c> to read. That
/// makes this interceptor scoped, and it is resolved from the scope that builds the context (see
/// <see cref="DbSetup"/>). Background jobs, seeding and the unit tests have no user and stamp a null
/// author.
/// </para>
/// </summary>
public sealed class StampInterceptor(TimeProvider timeProvider, ICurrentUser? currentUser = null)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var user = currentUser?.Name;

        // Materialised because turning a delete into a modification mutates the change tracker.
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    break;

                case EntityState.Deleted:
                    // Via Unchanged, not straight to Modified: flipping a delete to Modified marks every
                    // property modified, so deleting a key-only stub (db.Remove(new Event { Id = id }))
                    // would overwrite the whole row with that stub's defaults. Stamp columns only.
                    entry.State = EntityState.Unchanged;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    entry.Property(nameof(BaseEntity.DeletedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedBy)).IsModified = true;
                    break;
            }
        }
    }
}
