using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Configurations.Content;

public sealed class NavItemConfiguration : IEntityTypeConfiguration<NavItem>
{
    public void Configure(EntityTypeBuilder<NavItem> b)
    {
        b.Property(n => n.Location).HasConversion<string>().HasMaxLength(16);
        b.Property(n => n.LabelEn).HasMaxLength(128);
        b.Property(n => n.LabelAr).HasMaxLength(128);
        b.Property(n => n.Href).HasMaxLength(1024);

        // The header and each footer column are read whole, in order, on every page render.
        b.HasIndex(n => new { n.Location, n.SortOrder });
    }
}
