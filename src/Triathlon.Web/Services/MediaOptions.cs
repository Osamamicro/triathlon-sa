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
}
