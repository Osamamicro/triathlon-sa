using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Configurations.Content;

public sealed class PageBlockConfiguration : IEntityTypeConfiguration<PageBlock>
{
    public void Configure(EntityTypeBuilder<PageBlock> b)
    {
        b.HasIndex(x => new { x.PageId, x.SortOrder });

        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Variant).HasMaxLength(32);
        b.Property(x => x.Anchor).HasMaxLength(64);

        b.Property(x => x.EyebrowEn).HasMaxLength(256);
        b.Property(x => x.EyebrowAr).HasMaxLength(256);
        b.Property(x => x.TitleEn).HasMaxLength(256);
        b.Property(x => x.TitleAr).HasMaxLength(256);

        b.Property(x => x.CtaLabelEn).HasMaxLength(128);
        b.Property(x => x.CtaLabelAr).HasMaxLength(128);
        b.Property(x => x.CtaHref).HasMaxLength(1024);
        b.Property(x => x.SecondaryLabelEn).HasMaxLength(128);
        b.Property(x => x.SecondaryLabelAr).HasMaxLength(128);
        b.Property(x => x.SecondaryHref).HasMaxLength(1024);

        // BodyEn/Ar and ItemsJson are left unbounded: editor HTML and a serialised item list have
        // no length worth guessing, and both providers store that as their text type.

        // Items is computed from ItemsJson; EF would otherwise try to map the read-only property.
        b.Ignore(x => x.Items);
    }
}
