using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Configurations.Content;

public sealed class NewsPostConfiguration : IEntityTypeConfiguration<NewsPost>
{
    public void Configure(EntityTypeBuilder<NewsPost> b)
    {
        b.Property(p => p.Slug).HasMaxLength(128);
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.TitleEn).HasMaxLength(256);
        b.Property(p => p.TitleAr).HasMaxLength(256);
        b.Property(p => p.SummaryEn).HasMaxLength(1024);
        b.Property(p => p.SummaryAr).HasMaxLength(1024);
        b.Property(p => p.HeroImagePath).HasMaxLength(512);

        // BodyEn/Ar stay unbounded: an article is as long as it is.
        b.HasIndex(p => new { p.IsPublished, p.PublishedOn });
    }
}
