using Triathlon.Web.Services;

namespace Triathlon.Web.Infrastructure;

/// <summary>
/// Output caching for the public website.
/// <para>
/// Only endpoints that opt in with <c>[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy)]</c>
/// are cached, so the dashboard, the identity endpoints and the health check are outside it by
/// construction rather than by exclusion rules. The base policy the named one builds on already
/// refuses to cache anything but an anonymous GET or HEAD that returned 200 without setting a
/// cookie, which is what keeps a signed-in staff member's response out of a shared cache.
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
            // The base policy varies by nothing: an unknown query key (utm_source, fbclid) must not
            // create a second cache entry for the same page. Pages that read a query key declare it
            // with VaryByQueryKeys, which is applied after the base policy and wins.
            //
            // excludeDefaultPolicy: true here, because the default policy is what makes every
            // request eligible for caching in the first place — the base policy must add only the
            // vary-by-query rule, or every route in the app (dashboard included) would become
            // cacheable the moment it runs, opt-in attribute or not.
            //
            // The named policy repeats SetVaryByQuery([]): a named policy is itself built from its
            // own default policy (the one that varies by every query key by default), so without
            // repeating it here that default would win back over the base policy the moment a page
            // resolves "Public" by name.
            options.AddBasePolicy(policy => policy.SetVaryByQuery([]), excludeDefaultPolicy: true);
            options.AddPolicy(PublicPolicy, policy => policy.SetVaryByQuery([]).Expire(PublicLifetime).Tag(CacheTags.Site));
        });

        return services;
    }
}
