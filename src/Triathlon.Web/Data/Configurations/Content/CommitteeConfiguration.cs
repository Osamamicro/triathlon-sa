using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Configurations.Content;

public sealed class CommitteeConfiguration : IEntityTypeConfiguration<Committee>
{
    public void Configure(EntityTypeBuilder<Committee> b)
    {
        b.Property(c => c.KindEn).HasMaxLength(256);
        b.Property(c => c.KindAr).HasMaxLength(256);
        b.Property(c => c.NameEn).HasMaxLength(256);
        b.Property(c => c.NameAr).HasMaxLength(256);
        b.Property(c => c.DescriptionEn).HasMaxLength(1024);
        b.Property(c => c.DescriptionAr).HasMaxLength(1024);

        b.HasIndex(c => new { c.IsPublished, c.SortOrder });
    }
}
