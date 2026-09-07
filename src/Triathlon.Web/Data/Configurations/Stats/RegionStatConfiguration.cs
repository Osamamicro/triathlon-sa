using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Data.Configurations.Stats;

public sealed class RegionStatConfiguration : IEntityTypeConfiguration<RegionStat>
{
    public void Configure(EntityTypeBuilder<RegionStat> b)
    {
        b.Property(r => r.Key).HasMaxLength(64);
        b.Property(r => r.NameEn).HasMaxLength(128);
        b.Property(r => r.NameAr).HasMaxLength(128);
        b.HasIndex(r => r.Key).IsUnique();
    }
}
