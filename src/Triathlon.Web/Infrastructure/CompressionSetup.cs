using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Triathlon.Web.Infrastructure;

/// <summary>
/// Response compression for the public website.
/// <para>
/// Enabled over HTTPS. The BREACH class of attacks that made that a bad default needs a secret in
/// the response body and attacker-controlled input reflected beside it; the public pages are
/// anonymous and identical for every visitor, and the dashboard's antiforgery tokens sit behind
/// endpoints that output caching and this middleware both leave alone.
/// </para>
/// </summary>
public static class CompressionSetup
{
    public static IServiceCollection AddAppCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            // The defaults cover HTML, CSS, JS and plain text. The site also serves inline SVG
            // sprites and, from the content weeks, JSON endpoints — both compress very well.
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                ["image/svg+xml", "application/json"]);
        });

        // Fastest rather than Optimal: these are dynamic responses compressed on every miss, and
        // the extra CPU per request buys a few percent of size that the 60 s cache already amortises.
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

        return services;
    }
}
