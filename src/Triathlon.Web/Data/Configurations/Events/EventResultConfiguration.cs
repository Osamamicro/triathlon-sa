using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Data.Configurations.Events;

public sealed class EventResultConfiguration : IEntityTypeConfiguration<EventResult>
{
    public void Configure(EntityTypeBuilder<EventResult> b)
    {
        b.Property(r => r.AthleteEn).HasMaxLength(128);
        b.Property(r => r.AthleteAr).HasMaxLength(128);
        b.Property(r => r.ClubEn).HasMaxLength(128);
        b.Property(r => r.ClubAr).HasMaxLength(128);
        b.Property(r => r.Time).HasMaxLength(16);
        b.HasIndex(r => new { r.EventId, r.Position });
    }
}
