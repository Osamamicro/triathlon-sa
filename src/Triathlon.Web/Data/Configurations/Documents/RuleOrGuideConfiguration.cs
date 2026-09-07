using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Data.Configurations.Documents;

public sealed class RuleOrGuideConfiguration : IEntityTypeConfiguration<RuleOrGuide>
{
    public void Configure(EntityTypeBuilder<RuleOrGuide> b)
    {
        b.Property(r => r.Slug).HasMaxLength(128);
        b.HasIndex(r => r.Slug).IsUnique();
        b.Property(r => r.TitleEn).HasMaxLength(256);
        b.Property(r => r.TitleAr).HasMaxLength(256);
        b.Property(r => r.DescriptionEn).HasMaxLength(1024);
        b.Property(r => r.DescriptionAr).HasMaxLength(1024);
        b.Property(r => r.FilePath).HasMaxLength(512);
        b.Property(r => r.Audience).HasConversion<string>().HasMaxLength(64);
        b.HasIndex(r => new { r.IsPublished, r.SortOrder });
    }
}
