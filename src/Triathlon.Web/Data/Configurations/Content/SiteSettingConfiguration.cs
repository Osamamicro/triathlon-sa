using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Configurations.Content;

public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.Property(s => s.Key).HasMaxLength(64);
        b.HasIndex(s => s.Key).IsUnique();
        b.Property(s => s.ValueEn).HasMaxLength(1024);
        b.Property(s => s.ValueAr).HasMaxLength(1024);
    }
}
