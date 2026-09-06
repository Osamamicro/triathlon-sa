using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Identity;

namespace Triathlon.Web.Data.Configurations;

/// <summary>
/// The audit trail is read two ways — "what happened to this row" and "what happened lately" — so it
/// carries an index for each.
/// </summary>
public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.Property(log => log.User).HasMaxLength(256);
        builder.Property(log => log.Entity).HasMaxLength(128);
        builder.Property(log => log.EntityId).HasMaxLength(128);
        builder.Property(log => log.Action).HasMaxLength(64);

        builder.HasIndex(log => new { log.Entity, log.EntityId });
        builder.HasIndex(log => log.At);
    }
}
