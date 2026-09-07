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
            options.AddPolicy(PublicPolicy, policy => policy.Expire(PublicLifetime)));

        return services;
    }
}
