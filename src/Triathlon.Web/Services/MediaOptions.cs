namespace Triathlon.Web.Services;

/// <summary>Where uploaded media lives and how it is addressed from a browser.</summary>
public sealed class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>
    /// Root directory for uploads. Empty means <c>wwwroot/media</c> under the content root; a
    /// relative value is resolved against the content root; an absolute value is used as given.
    /// On a real server this points outside the deployment folder so a redeploy cannot wipe it.
    /// </summary>
    public string Root { get; set; } = string.Empty;

    /// <summary>URL prefix the stored files are served under. Must start with "/".</summary>
    public string PublicPrefix { get; set; } = "/media";

    /// <summary><see cref="PublicPrefix"/> with exactly one leading slash and none trailing.</summary>
    public string NormalizedPublicPrefix => "/" + PublicPrefix.Trim('/');

    /// <summary>
    /// Resolves <see cref="Root"/> against <paramref name="environment"/> the same way for every
    /// consumer — the file store that writes uploads and the middleware that serves them back must
    /// agree on exactly one directory.
    /// </summary>
    public string ResolveRoot(IWebHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(Root))
        {
            return Path.Combine(environment.ContentRootPath, "wwwroot", "media");
        }

        return Path.IsPathRooted(Root)
            ? Root
            : Path.Combine(environment.ContentRootPath, Root);
    }
}
