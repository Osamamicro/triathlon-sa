using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Data.Configurations.Events;

public sealed class EventGalleryImageConfiguration : IEntityTypeConfiguration<EventGalleryImage>
{
    public void Configure(EntityTypeBuilder<EventGalleryImage> b)
    {
        b.Property(g => g.Path).HasMaxLength(512);
        b.Property(g => g.AltEn).HasMaxLength(256);
        b.Property(g => g.AltAr).HasMaxLength(256);
        b.HasIndex(g => new { g.EventId, g.SortOrder });
    }
}
