using Microsoft.EntityFrameworkCore;

namespace Triathlon.Web.Data;

/// <summary>PostgreSQL flavour of <see cref="AppDbContext"/>; owns <c>Data/Migrations/Postgres</c>.</summary>
public sealed class PostgresDbContext(DbContextOptions<PostgresDbContext> options) : AppDbContext(options);
