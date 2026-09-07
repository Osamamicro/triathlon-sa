using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Data.Configurations.Events;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        b.Property(c => c.Key).HasMaxLength(64);
        b.Property(c => c.NameEn).HasMaxLength(128);
        b.Property(c => c.NameAr).HasMaxLength(128);
        b.HasIndex(c => c.Key).IsUnique();
    }
}
