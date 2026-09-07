using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Crm;

namespace Triathlon.Web.Data.Configurations.Crm;

public sealed class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> b)
    {
        b.Property(c => c.NameEn).HasMaxLength(128);
        b.Property(c => c.NameAr).HasMaxLength(128);
        b.Property(c => c.CityEn).HasMaxLength(128);
        b.Property(c => c.CityAr).HasMaxLength(128);

        b.HasIndex(c => new { c.IsActive, c.SortOrder });
    }
}
