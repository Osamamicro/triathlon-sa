namespace Triathlon.Web.Data;

/// <summary>
/// Configuration lookup shared by the design-time factories. EF tooling prefers an
/// <see cref="Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory{TContext}"/> over building the
/// application host, so a factory that hardcodes its connection string makes every <c>dotnet ef</c> command —
/// <c>database update</c> against staging or production included — silently target that hardcoded database.
/// Reading configuration here keeps the tooling pointed wherever <c>ConnectionStrings:Default</c> (or the
/// <c>ConnectionStrings__Default</c> environment variable) says, while the per-provider fallback keeps offline
/// <c>migrations add</c> working with no server and no settings file.
/// </summary>
public static class DesignTimeConfiguration
{
    /// <summary>
    /// Returns <c>ConnectionStrings:Default</c> from <c>appsettings.json</c>, the environment-specific
    /// overlay and environment variables, or <paramref name="fallback"/> when none of them supply one.
    /// <paramref name="basePath"/> defaults to the working directory, which is the project directory
    /// <c>dotnet ef</c> runs from; tests pass their own.
    /// </summary>
    public static string ConnectionStringOrFallback(string fallback, string? basePath = null)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath ?? Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true);

        if (!string.IsNullOrWhiteSpace(environment))
        {
            builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
        }

        var connectionString = builder.AddEnvironmentVariables().Build().GetConnectionString("Default");

        return string.IsNullOrWhiteSpace(connectionString) ? fallback : connectionString;
    }
}
