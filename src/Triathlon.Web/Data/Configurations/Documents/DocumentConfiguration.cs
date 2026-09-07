using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Data.Configurations.Documents;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.Property(d => d.TitleEn).HasMaxLength(256);
        b.Property(d => d.TitleAr).HasMaxLength(256);
        b.Property(d => d.FilePath).HasMaxLength(512);
        b.Property(d => d.Category).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(d => new { d.IsPublished, d.Category, d.Year });
    }
}
