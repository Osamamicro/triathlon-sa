using Microsoft.EntityFrameworkCore;

namespace Triathlon.Web.Data;

/// <summary>SQL Server flavour of <see cref="AppDbContext"/>; owns <c>Data/Migrations/SqlServer</c>.</summary>
public sealed class SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : AppDbContext(options);
