using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Media;

namespace Triathlon.Web.Data.Configurations.Media;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.Property(m => m.Kind).HasConversion<string>().HasMaxLength(16);
        b.Property(m => m.Path).HasMaxLength(512);
        b.HasIndex(m => m.Path).IsUnique();
        b.Property(m => m.ContentType).HasMaxLength(64);
        b.Property(m => m.OriginalFileName).HasMaxLength(256);
        b.Property(m => m.AltEn).HasMaxLength(256);
        b.Property(m => m.AltAr).HasMaxLength(256);
        b.HasIndex(m => new { m.Kind, m.CreatedAt });

        // Variants is computed from VariantsJson; EF would otherwise try to map the read-only property.
        b.Ignore(m => m.Variants);
    }
}
