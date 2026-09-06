using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Common;

namespace Triathlon.Tests.Data;

/// <summary>A throwaway entity that only exists so the shared soft-delete behaviour can be tested.</summary>
public sealed class Probe : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Test-only context: inherits every convention from <see cref="AppDbContext"/> and adds the probe table.</summary>
public sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : AppDbContext(options)
{
    public DbSet<Probe> Probes => Set<Probe>();
}

public sealed class SoftDeleteTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private ProbeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProbeDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(new StampInterceptor(TimeProvider.System))
            .Options;

        return new ProbeDbContext(options);
    }

    [Fact]
    public async Task Deleting_hides_entity_and_sets_DeletedAt()
    {
        var id = Guid.CreateVersion7();

        await using (var db = CreateContext())
        {
            db.Probes.Add(new Probe { Id = id, Name = "Jeddah Sprint" });
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext())
        {
            var probe = await db.Probes.SingleAsync(p => p.Id == id);
            db.Probes.Remove(probe);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext())
        {
            Assert.Null(await db.Probes.FirstOrDefaultAsync(p => p.Id == id));

            var deleted = await db.Probes.IgnoreQueryFilters().SingleAsync(p => p.Id == id);
            Assert.NotNull(deleted.DeletedAt);
            Assert.NotNull(deleted.UpdatedAt);
        }
    }

    [Fact]
    public async Task Adding_sets_CreatedAt()
    {
        var id = Guid.CreateVersion7();

        await using (var db = CreateContext())
        {
            db.Probes.Add(new Probe { Id = id, Name = "Riyadh Olympic" });
            await db.SaveChangesAsync();
        }

        await using (var db = CreateContext())
        {
            var probe = await db.Probes.SingleAsync(p => p.Id == id);
            Assert.NotEqual(default, probe.CreatedAt);
            Assert.Null(probe.UpdatedAt);
            Assert.Null(probe.DeletedAt);
        }
    }
}
