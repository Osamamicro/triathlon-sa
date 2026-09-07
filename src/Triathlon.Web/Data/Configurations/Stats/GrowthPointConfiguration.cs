using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triathlon.Web.Domain.Stats;

namespace Triathlon.Web.Data.Configurations.Stats;

public sealed class GrowthPointConfiguration : IEntityTypeConfiguration<GrowthPoint>
{
    public void Configure(EntityTypeBuilder<GrowthPoint> b)
    {
        b.HasIndex(g => g.Year).IsUnique();
    }
}
