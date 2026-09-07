using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Triathlon.Web.Services;

namespace Triathlon.Web.Infrastructure;

/// <summary>
/// Serves uploaded media straight off disk.
/// <para>
/// <c>MapStaticAssets()</c> in .NET 10 only serves the build-time manifest, so a file written to
/// the media root at runtime 404s under it. In production <c>deploy/nginx.conf</c> short-circuits
/// everything under the media prefix before it reaches the application, so this middleware never
/// runs there — but it is what makes uploads visible in every deployment that is not behind that
/// nginx config: a direct-Kestrel or IIS site, a developer machine, and the integration tests.
/// </para>
/// </summary>
public static class MediaFilesSetup
{
    public static WebApplication UseMediaFiles(this WebApplication app)
    {
        var media = app.Services.GetRequiredService<IOptions<MediaOptions>>().Value;
        var root = media.ResolveRoot(app.Environment);

        // The file store only creates {root}/{yyyy}/{MM} on the first upload; the static file
        // middleware needs the root itself to exist at startup.
        Directory.CreateDirectory(root);

        // nosniff is not set here: SecurityHeadersMiddleware runs ahead of this in the pipeline and
        // puts it on every response, and appending a second copy would send the header twice.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = media.NormalizedPublicPrefix,
            ServeUnknownFileTypes = false,
        });

        return app;
    }
}
