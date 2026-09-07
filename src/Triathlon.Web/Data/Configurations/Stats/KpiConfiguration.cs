using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Data.Configurations.Stats;

public sealed class KpiConfiguration : IEntityTypeConfiguration<Kpi>
{
    public void Configure(EntityTypeBuilder<Kpi> b)
    {
        b.Property(k => k.Key).HasMaxLength(64);
        b.Property(k => k.LabelEn).HasMaxLength(128);
        b.Property(k => k.LabelAr).HasMaxLength(128);
        b.Property(k => k.Suffix).HasMaxLength(8);
        b.Property(k => k.NoteEn).HasMaxLength(64);
        b.Property(k => k.NoteAr).HasMaxLength(64);
        b.Property(k => k.Color).HasMaxLength(16);
        b.Property(k => k.Source).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(k => k.Key).IsUnique();
    }
}
