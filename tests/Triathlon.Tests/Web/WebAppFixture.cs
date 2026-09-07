using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// Boots the real application against a throwaway PostgreSQL container, with startup migration and
/// identity seeding switched on, so the tests exercise the same wiring production uses.
/// </summary>
public sealed class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "seed-admin@triathlon.test";
    public const string AdminPassword = "Seed-Admin-Pass-2026!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>A rate-limited POST. No public form exists yet, so the policy needs a target.</summary>
    public const string RateLimitedPath = "/__test/limited";

    /// <summary>
    /// A cache-tagged GET whose body changes on every render, which is what makes a cache hit — and
    /// an eviction — provable rather than inferred from headers.
    /// </summary>
    public const string CacheTaggedPath = "/__test/cache-tagged";

    /// <summary>
    /// Always throws. Used to prove security headers survive <c>UseExceptionHandler</c>'s
    /// <c>Response.Clear()</c> — only reachable on a host that registers the exception handler,
    /// i.e. one built outside the Development environment.
    /// </summary>
    public const string ThrowingPath = "/__test/throw";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Program.ConfigureTestEndpoints is a static hook, so this must be set before any host is
        // built — including the ones WithWebHostBuilder creates for a differently configured site.
        Program.ConfigureTestEndpoints = MapTestEndpoints;

        // Touching Services builds the host, which is what migrates and seeds.
        _ = Services;
    }

    /// <summary>
    /// Endpoints production does not have, wired exactly the way the real ones will be: the same
    /// named rate-limiting policy, and the same output-cache policy and tag the home page carries.
    /// </summary>
    private static void MapTestEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(RateLimitedPath, () => Results.Ok("accepted"))
            .RequireRateLimiting(RateLimitSetup.PublicPost)
            .DisableAntiforgery();

        endpoints.MapGet(CacheTaggedPath, () => Guid.NewGuid().ToString("N"))
            .WithMetadata(new OutputCacheAttribute
            {
                PolicyName = OutputCacheSetup.PublicPolicy,
                Tags = [CacheTags.Home],
            });

        endpoints.MapGet(ThrowingPath, ThrowingHandler);
    }

    private static Task ThrowingHandler(HttpContext context) =>
        throw new InvalidOperationException("Test endpoint always throws.");

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Database:Provider"] = "Postgres",
                ["Database:MigrateOnStartup"] = "true",
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
            }));
    }

    /// <summary>
    /// The identity cookie is marked Secure (see Program.cs), so a client that talks to the test
    /// host over plain "http://localhost" never gets it back on the next request. Tests that need
    /// the cookie to round-trip (anything that signs in and then reuses the client) must create
    /// their client with this as the <see cref="WebApplicationFactoryClientOptions.BaseAddress"/> —
    /// TestServer accepts "https" without a real TLS handshake, so this is otherwise a normal client.
    /// </summary>
    public static readonly Uri HttpsBaseAddress = new("https://localhost");
}

/// <summary>Shares one container and one application host across every web test class.</summary>
[CollectionDefinition(Name)]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "web-app";
}
