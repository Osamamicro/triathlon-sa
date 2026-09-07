using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Triathlon.Web.Data;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// Boots the real application against a throwaway database container, with startup migration and
/// identity seeding switched on, so the tests exercise the same wiring production uses. The provider
/// defaults to PostgreSQL; set <c>STF_TEST_DB=SqlServer</c> to run the same suite against a SQL Server
/// container instead (CI does both, in separate jobs).
/// </summary>
public sealed class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "seed-admin@triathlon.test";
    public const string AdminPassword = "Seed-Admin-Pass-2026!";

    private static readonly bool UseSqlServer = string.Equals(
        Environment.GetEnvironmentVariable("STF_TEST_DB"), "SqlServer", StringComparison.OrdinalIgnoreCase);

    private readonly PostgreSqlContainer? _postgres = UseSqlServer ? null : new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly MsSqlContainer? _sqlServer = UseSqlServer ? new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build() : null;

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

    private readonly List<EmailMessage> _sentEmails = [];

    /// <summary>
    /// Every message the application handed to <see cref="IEmailSender"/> since the host started —
    /// a snapshot, taken under the same lock the recorder writes with, so a test can enumerate it
    /// while another test's request is still sending.
    /// </summary>
    public List<EmailMessage> Emails
    {
        get
        {
            lock (_sentEmails)
            {
                return [.. _sentEmails];
            }
        }
    }

    public async Task InitializeAsync()
    {
        if (_sqlServer is not null)
        {
            await _sqlServer.StartAsync();
        }
        else
        {
            await _postgres!.StartAsync();
        }

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

        if (_sqlServer is not null)
        {
            await _sqlServer.DisposeAsync();
        }
        else
        {
            await _postgres!.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // TestServer gives every request the same (absent) remote address, which would put every
            // form post in one rate-limiting bucket and make the tests interfere with each other.
            services.AddTransient<IStartupFilter, TestClientIp>();

            // No SMTP host is configured here, so the application would otherwise resolve the
            // logging sender and a test could only assert that nothing threw. The recorder keeps the
            // messages instead, which is what lets a registration test read the mail it caused.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(new RecordingEmailSender(_sentEmails));
        });

        var connectionString = _sqlServer is not null
            ? $"{_sqlServer.GetConnectionString()};TrustServerCertificate=True"
            : _postgres!.GetConnectionString();
        var provider = _sqlServer is not null ? DbSetup.SqlServer : DbSetup.Postgres;

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Database:Provider"] = provider,
                ["Database:MigrateOnStartup"] = "true",
                ["Database:SeedContent"] = "true",
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

/// <summary>
/// Keeps every outgoing message instead of sending it, so a test can assert on the mail a request
/// produced. The list is owned by the fixture and shared with every host it builds; both sides lock
/// on it, because requests run concurrently and a test may read while one is still in flight.
/// </summary>
public sealed class RecordingEmailSender(List<EmailMessage> sent) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sent);

        lock (sent)
        {
            sent.Add(message);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Shares one container and one application host across every web test class.</summary>
[CollectionDefinition(Name)]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "web-app";
}
