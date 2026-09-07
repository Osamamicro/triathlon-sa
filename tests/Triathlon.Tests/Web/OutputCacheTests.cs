using System.Net;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

/// <summary>
/// Output caching is what lets one modest server absorb a race-day traffic spike, and tag eviction
/// is what stops it from showing an editor stale content a minute after they published. These cover
/// both halves: a repeat request is served from the cache, and evicting the tag ends that.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class OutputCacheTests(WebAppFixture app)
{
    [Fact]
    public async Task Repeat_request_is_served_from_the_cache()
    {
        using var client = app.CreateClient();

        // The endpoint returns a fresh GUID per render, so an identical body can only be a cache hit.
        var first = await client.GetStringAsync(WebAppFixture.CacheTaggedPath);
        var second = await client.GetStringAsync(WebAppFixture.CacheTaggedPath);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Evicting_the_tag_makes_the_next_request_render_again()
    {
        using var client = app.CreateClient();

        var cached = await client.GetStringAsync(WebAppFixture.CacheTaggedPath);
        Assert.Equal(cached, await client.GetStringAsync(WebAppFixture.CacheTaggedPath));

        // This is the call a dashboard save makes after committing.
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();
            await store.EvictAsync(CancellationToken.None, CacheTags.Home);
        }

        Assert.NotEqual(cached, await client.GetStringAsync(WebAppFixture.CacheTaggedPath));
    }

    [Fact]
    public async Task Home_page_is_cached_and_dropped_by_the_home_tag()
    {
        using var client = app.CreateClient();

        using var first = await client.GetAsync("/en");
        using var second = await client.GetAsync("/en");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(
            await first.Content.ReadAsStringAsync(),
            await second.Content.ReadAsStringAsync());

        // The repeat is a replay, not a re-render: only a response served out of the output cache
        // carries an Age header.
        Assert.True(second.Headers.Contains("Age"), "The second GET /en was not served from the output cache.");

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();
            await store.EvictAsync(CancellationToken.None, CacheTags.Home);
        }

        using var afterEviction = await client.GetAsync("/en");

        Assert.Equal(HttpStatusCode.OK, afterEviction.StatusCode);
        Assert.False(afterEviction.Headers.Contains("Age"), "GET /en was still cached after the tag was evicted.");
    }

    [Fact]
    public async Task Dashboard_is_never_cached()
    {
        using var client = app.CreateClient();

        using var first = await client.GetAsync("/dashboard/login");
        using var second = await client.GetAsync("/dashboard/login");

        // Nothing under /dashboard opts into a cache policy, so neither response can be a replay.
        Assert.False(first.Headers.Contains("Age"));
        Assert.False(second.Headers.Contains("Age"));
    }
}
