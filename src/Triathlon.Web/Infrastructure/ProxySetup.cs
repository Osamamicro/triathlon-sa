using Microsoft.AspNetCore.HttpOverrides;

namespace Triathlon.Web.Infrastructure;

/// <summary>
/// Teaches the application to read the client's real address and scheme from a reverse proxy.
/// <para>
/// Only switched on by <c>Site:BehindProxy</c>, and when it is, the known-proxy allow list is
/// cleared: on the Federation's server Kestrel binds to 127.0.0.1 only, so nginx (or IIS) is the
/// sole thing that can reach it and every X-Forwarded-* header it sees was written by that proxy.
/// If Kestrel is ever exposed directly, this must be turned off — otherwise any caller could spoof
/// its own address and defeat the public rate limiter. The same trust note lives in deploy/README.md.
/// </para>
/// </summary>
public static class ProxySetup
{
    public static IServiceCollection AddAppProxy(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue($"{SiteOptions.SectionName}:BehindProxy", false))
        {
            return services;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static WebApplication UseAppProxy(this WebApplication app)
    {
        if (app.Configuration.GetValue($"{SiteOptions.SectionName}:BehindProxy", false))
        {
            app.UseForwardedHeaders();
        }

        return app;
    }
}
