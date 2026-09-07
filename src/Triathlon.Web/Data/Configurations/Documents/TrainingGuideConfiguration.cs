using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Data.Configurations.Documents;

public sealed class TrainingGuideConfiguration : IEntityTypeConfiguration<TrainingGuide>
{
    public void Configure(EntityTypeBuilder<TrainingGuide> b)
    {
        b.Property(g => g.Slug).HasMaxLength(128);
        b.HasIndex(g => g.Slug).IsUnique();
        b.Property(g => g.TitleEn).HasMaxLength(256);
        b.Property(g => g.TitleAr).HasMaxLength(256);
        b.Property(g => g.SummaryEn).HasMaxLength(1024);
        b.Property(g => g.SummaryAr).HasMaxLength(1024);
        b.Property(g => g.LevelEn).HasMaxLength(64);
        b.Property(g => g.LevelAr).HasMaxLength(64);
        b.Property(g => g.FilePath).HasMaxLength(512);
        b.HasIndex(g => new { g.IsPublished, g.SortOrder });
        b.HasMany(g => g.Chapters).WithOne().HasForeignKey(c => c.TrainingGuideId).OnDelete(DeleteBehavior.Cascade);
    }
}
