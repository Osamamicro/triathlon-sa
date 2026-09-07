using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Common;
using Triathlon.Web.Domain.Content;
using Triathlon.Web.Domain.Crm;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Domain.Events;
using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Data;

/// <summary>
/// Provider-agnostic application context. It is never registered directly: the provider-specific
/// subclasses (<see cref="PostgresDbContext"/>, <see cref="SqlServerDbContext"/>) exist so that each
/// provider owns its own migration set, since EF matches migrations to a context type.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser>
{
    protected AppDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>Append-only audit trail; never soft-deleted, so it is exempt from the filter below.</summary>
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<City> Cities => Set<City>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventGalleryImage> EventGalleryImages => Set<EventGalleryImage>();
    public DbSet<EventResult> EventResults => Set<EventResult>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<RuleOrGuide> Rules => Set<RuleOrGuide>();
    public DbSet<TrainingGuide> TrainingGuides => Set<TrainingGuide>();
    public DbSet<TrainingGuideChapter> TrainingGuideChapters => Set<TrainingGuideChapter>();

    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageBlock> PageBlocks => Set<PageBlock>();
    public DbSet<NavItem> NavItems => Set<NavItem>();
    public DbSet<Committee> Committees => Set<Committee>();
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Athlete> Athletes => Set<Athlete>();

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
