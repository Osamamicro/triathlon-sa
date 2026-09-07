using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Data.Configurations.Documents;

public sealed class TrainingGuideChapterConfiguration : IEntityTypeConfiguration<TrainingGuideChapter>
{
    public void Configure(EntityTypeBuilder<TrainingGuideChapter> b)
    {
        b.Property(c => c.TitleEn).HasMaxLength(256);
        b.Property(c => c.TitleAr).HasMaxLength(256);
        b.HasIndex(c => new { c.TrainingGuideId, c.SortOrder });
    }
}
