using System.Net;

namespace Triathlon.Tests.Web;

/// <summary>
/// The public site will accept anonymous form posts — registrations, contact — so the limiter that
/// keeps one source from flooding them has to be in the pipeline and reachable from endpoint
/// metadata before the first of those forms ships.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class RateLimitTests(WebAppFixture app)
{
    [Fact]
    public async Task Eleventh_post_in_the_window_is_refused()
    {
        using var client = app.CreateClient();

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            using var allowed = await client.PostAsync(WebAppFixture.RateLimitedPath, content: null);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var refused = await client.PostAsync(WebAppFixture.RateLimitedPath, content: null);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);

        // A refused caller is told when to come back rather than left to guess.
        Assert.True(refused.Headers.Contains("Retry-After"), "The 429 carried no Retry-After header.");
    }
}
