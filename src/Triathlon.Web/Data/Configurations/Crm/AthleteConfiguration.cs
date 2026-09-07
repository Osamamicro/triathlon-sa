using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Crm;

namespace Triathlon.Web.Data.Configurations.Crm;

public sealed class AthleteConfiguration : IEntityTypeConfiguration<Athlete>
{
    public void Configure(EntityTypeBuilder<Athlete> b)
    {
        b.Property(a => a.FullName).HasMaxLength(128);
        b.Property(a => a.Email).HasMaxLength(256);
        b.Property(a => a.Category).HasMaxLength(64);
        b.Property(a => a.CityKey).HasMaxLength(64);
        b.Property(a => a.PreferredCulture).HasMaxLength(8);
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);

        // Not unique yet: a filtered unique index is spelled differently on each provider, and the
        // licence number is only issued by the Week 5 membership flow, which adds it with its own
        // migration. Until then every row here is Pending and the column is null.
        b.Property(a => a.LicenceNumber).HasMaxLength(32);

        b.HasIndex(a => a.Status);
        b.HasIndex(a => a.Email);
    }
}
