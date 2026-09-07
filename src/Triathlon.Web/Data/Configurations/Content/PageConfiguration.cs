using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Configurations.Content;

public sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> b)
    {
        b.Property(p => p.Slug).HasMaxLength(128);
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.TitleEn).HasMaxLength(256);
        b.Property(p => p.TitleAr).HasMaxLength(256);
        b.Property(p => p.MetaDescriptionEn).HasMaxLength(1024);
        b.Property(p => p.MetaDescriptionAr).HasMaxLength(1024);

        // Blocks have no life of their own: deleting a page deletes its sections.
        b.HasMany(p => p.Blocks).WithOne().HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.Cascade);
    }
}
