namespace Triathlon.Web.Infrastructure;

/// <summary>How this particular deployment of the site is exposed.</summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    /// <summary>
    /// True on the staging site. It puts the whole site behind HTTP Basic authentication and makes
    /// the layout emit <c>noindex</c>, so the client can review work in progress without it being
    /// crawled or stumbled upon.
    /// </summary>
    public bool Staging { get; set; }

    /// <summary>
    /// True when a reverse proxy (nginx, or IIS's ARR) terminates TLS in front of Kestrel. See
    /// <c>deploy/README.md</c> for the trust assumption this carries.
    /// </summary>
    public bool BehindProxy { get; set; }

    /// <summary>The staging gate's credentials. Ignored unless <see cref="Staging"/> is true.</summary>
    public BasicAuthOptions BasicAuth { get; set; } = new();
}

/// <summary>The single shared credential the staging gate accepts.</summary>
public sealed class BasicAuthOptions
{
    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
