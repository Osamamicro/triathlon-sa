using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Triathlon.Tests.Web;

/// <summary>
/// The headers are the only defence that is on for every response whether or not a page remembers
/// to ask for it, so they are asserted per area: the baseline everywhere, the strict script rule on
/// the public site, and the deliberate Hangfire exemption.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class SecurityHeadersTests(WebAppFixture app)
{
    [Theory]
    [InlineData("/en")]
    [InlineData("/dashboard/login")]
    public async Task Baseline_headers_are_present(string path)
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("camera=()", response.Headers.GetValues("Permissions-Policy").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_pages_forbid_inline_scripts()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/en");
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();

        Assert.Contains("script-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-inline'", csp.Split("style-src")[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dashboard_pages_carry_no_inline_script()
    {
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/dashboard/login");

        // script-src 'self' blocks every inline script, including the <script type="importmap">
        // Blazor's <ImportMap /> renders — the browser reports that as a violation and skips it, so
        // the shell must not emit one at all.
        Assert.DoesNotContain("<script type=\"importmap\">", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hangfire_dashboard_is_exempt_from_the_application_csp()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/dashboard/jobs");

        // Anonymous is refused by Hangfire's own authorisation filter; the point is the header
        // decision on that path. Hangfire renders inline scripts and sends its own narrow policy
        // ("frame-ancestors 'self'"), so this middleware must not stamp script-src 'self' over it —
        // that would blank its dashboard for the one administrator allowed to see it.
        var csp = string.Join("; ", response.Headers.GetValues("Content-Security-Policy"));

        Assert.DoesNotContain("script-src", csp, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_host_is_refused_when_AllowedHosts_is_set()
    {
        using var pinned = app.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(
            new Dictionary<string, string?> { ["AllowedHosts"] = "triathlon.sa;www.triathlon.sa" })));
        using var client = pinned.CreateClient();

        using var refused = await client.GetAsync("http://evil.example/en");
        using var allowed = await client.GetAsync("http://triathlon.sa/en");

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    /// <summary>
    /// <c>UseExceptionHandler</c> — only registered outside Development — calls
    /// <c>Response.Clear()</c> before re-executing to the error page, which used to wipe every
    /// header this middleware had already written. The fixture's own host runs in Development
    /// (where the exception handler is not wired at all, so this bug could not show up there), so
    /// this spins up a second host in Production, the way the real deployment runs.
    /// </summary>
    [Fact]
    public async Task Security_headers_survive_the_response_clear_on_an_unhandled_exception()
    {
        using var production = app.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var client = production.CreateClient();

        using var response = await client.GetAsync(WebAppFixture.ThrowingPath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        // The thrown path is public (not under /dashboard), so the exception handler re-executes to
        // the branded /en/error page.
        Assert.Contains("class=\"site-header\"", html, StringComparison.Ordinal);
    }
}
