using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// The challenge is only worth the server-side half of it, so the half that decides is pinned here:
/// Cloudflare's answer is believed when it says yes, and everything else — a rejection, an error
/// status, a torn connection — is a no. A verifier that failed open would let a bot through exactly
/// when Cloudflare is having a bad day, which is when it matters.
/// </summary>
public sealed class TurnstileVerifierTests
{
    [Fact]
    public async Task A_token_Cloudflare_accepts_is_verified()
    {
        var verifier = Verifier(new FakeHandler(HttpStatusCode.OK, "{\"success\":true}"));

        Assert.True(await verifier.VerifyAsync("token", "10.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task A_token_Cloudflare_rejects_is_not_verified()
    {
        var verifier = Verifier(new FakeHandler(HttpStatusCode.OK, "{\"success\":false,\"error-codes\":[\"invalid-input-response\"]}"));

        Assert.False(await verifier.VerifyAsync("token", null, CancellationToken.None));
    }

    [Fact]
    public async Task An_unreachable_endpoint_fails_closed()
    {
        var verifier = Verifier(new FakeHandler(new HttpRequestException("Connection refused.")));

        Assert.False(await verifier.VerifyAsync("token", null, CancellationToken.None));
    }

    [Fact]
    public async Task An_error_status_fails_closed()
    {
        var verifier = Verifier(new FakeHandler(HttpStatusCode.InternalServerError, "nope"));

        Assert.False(await verifier.VerifyAsync("token", null, CancellationToken.None));
    }

    [Fact]
    public async Task A_missing_token_is_refused_without_asking_Cloudflare()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "{\"success\":true}");
        var verifier = Verifier(handler);

        Assert.False(await verifier.VerifyAsync("", null, CancellationToken.None));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task The_verifier_used_when_no_keys_are_configured_lets_everything_through()
    {
        Assert.True(await new NoopTurnstileVerifier().VerifyAsync(null, null, CancellationToken.None));
    }

    [Fact]
    public void A_half_configured_key_pair_leaves_the_challenge_off()
    {
        Assert.False(new TurnstileOptions { SiteKey = "site" }.Enabled);
        Assert.False(new TurnstileOptions { SecretKey = "secret" }.Enabled);
        Assert.True(new TurnstileOptions { SiteKey = "site", SecretKey = "secret" }.Enabled);
    }

    private static TurnstileVerifier Verifier(FakeHandler handler) =>
        new(new SingleClientFactory(handler),
            Options.Create(new TurnstileOptions { SiteKey = "site", SecretKey = "secret" }),
            NullLogger<TurnstileVerifier>.Instance);

    /// <summary>Answers every request the same way, or throws the same way, and counts the attempts.</summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body = "";
        private readonly Exception? _throws;

        public FakeHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public FakeHandler(Exception throws) => _throws = throws;

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;

            if (_throws is not null)
            {
                return Task.FromException<HttpResponseMessage>(_throws);
            }

            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
