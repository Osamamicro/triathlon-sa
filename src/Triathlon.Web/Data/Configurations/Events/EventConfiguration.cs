using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Data.Configurations.Events;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.Property(e => e.Slug).HasMaxLength(128);
        b.HasIndex(e => e.Slug).IsUnique();
        b.Property(e => e.Season).HasMaxLength(16);
        b.Property(e => e.TitleEn).HasMaxLength(256);
        b.Property(e => e.TitleAr).HasMaxLength(256);
        b.Property(e => e.VenueEn).HasMaxLength(256);
        b.Property(e => e.VenueAr).HasMaxLength(256);
        b.Property(e => e.SwimDistance).HasMaxLength(32);
        b.Property(e => e.BikeDistance).HasMaxLength(32);
        b.Property(e => e.RunDistance).HasMaxLength(32);
        b.Property(e => e.Categories).HasMaxLength(512);
        b.Property(e => e.ExternalRegistrationUrl).HasMaxLength(1024);
        b.Property(e => e.HeroImagePath).HasMaxLength(512);
        b.Property(e => e.ResultsFilePath).HasMaxLength(512);
        b.Property(e => e.Type).HasConversion<string>().HasMaxLength(16);
        b.Property(e => e.RegistrationMode).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(e => new { e.IsPublished, e.DateStart });
        b.HasIndex(e => e.Season);
        b.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(e => e.Gallery).WithOne().HasForeignKey(g => g.EventId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(e => e.Results).WithOne().HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(e => e.Registrations).WithOne().HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Restrict);
    }
}
