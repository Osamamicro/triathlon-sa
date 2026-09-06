using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Data;

/// <summary>
/// Provider-agnostic application context. It is never registered directly: the provider-specific
/// subclasses (<see cref="PostgresDbContext"/>, <see cref="SqlServerDbContext"/>) exist so that each
/// provider owns its own migration set, since EF matches migrations to a context type.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    protected AppDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Materialised: HasQueryFilter mutates the model while we walk it.
        foreach (var entityType in builder.Model.GetEntityTypes().ToList())
        {
            if (entityType.BaseType is not null || entityType.IsOwned())
            {
                continue;
            }

            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            builder.Entity(entityType.ClrType).HasQueryFilter(NotSoftDeleted(entityType.ClrType));
        }
    }

    /// <summary>Builds <c>e =&gt; e.DeletedAt == null</c> for an arbitrary <see cref="BaseEntity"/> type.</summary>
    private static LambdaExpression NotSoftDeleted(Type clrType)
    {
        var entity = Expression.Parameter(clrType, "e");
        var deletedAt = Expression.Property(entity, nameof(BaseEntity.DeletedAt));
        var body = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?)));

        return Expression.Lambda(body, entity);
    }
}
