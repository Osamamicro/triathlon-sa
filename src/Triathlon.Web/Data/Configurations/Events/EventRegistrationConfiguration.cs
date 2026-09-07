using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Data.Configurations.Events;

public sealed class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> b)
    {
        b.Property(r => r.FullName).HasMaxLength(128);
        b.Property(r => r.Email).HasMaxLength(256);
        b.Property(r => r.Phone).HasMaxLength(32);
        b.Property(r => r.Category).HasMaxLength(64);
        b.Property(r => r.Club).HasMaxLength(128);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(r => new { r.EventId, r.Status });
    }
}
