using Triathlon.Web.Services;

namespace Triathlon.Web.Infrastructure;

/// <summary>
/// Output caching for the public website.
/// <para>
/// Only endpoints that opt in with <c>[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy)]</c>
/// are cached, so the dashboard, the identity endpoints and the health check are outside it by
/// construction rather than by exclusion rules. The framework's default policy, which the named
/// policy below builds on, already refuses to cache anything but an anonymous GET or HEAD that
/// returned 200 without setting a cookie, which is what keeps a signed-in staff member's response
/// out of a shared cache.
/// </para>
/// <para>
/// Every stored response also carries <see cref="CacheTags.Site"/>, on top of whatever content
/// tags the page itself declares, so a change to something the whole site renders — the
/// navigation, a site setting — is one eviction rather than one per page.
/// </para>
/// </summary>
public static class OutputCacheSetup
{
    /// <summary>Policy name for cacheable public pages.</summary>
    public const string PublicPolicy = "Public";

    /// <summary>
    /// Short on purpose: the dashboard evicts by tag the moment content is published, so this is
    /// only the backstop for anything that forgets to, and a minute of staleness is the worst case.
    /// </summary>
    public static readonly TimeSpan PublicLifetime = TimeSpan.FromSeconds(60);

    public static IServiceCollection AddAppOutputCache(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            // A named policy is itself built from the framework's own default policy, which varies
            // by every query key ("*"). SetVaryByQuery([]) here resets that back to none, so an
            // unknown query key (utm_source, fbclid) does not create a second cache entry for the
            // same page. A page's own VaryByQueryKeys (applied by [OutputCache] after this named
            // policy runs) still wins for the keys it actually declares.
            options.AddPolicy(PublicPolicy, policy => policy.SetVaryByQuery([]).Expire(PublicLifetime).Tag(CacheTags.Site));
        });

        return services;
    }
}
