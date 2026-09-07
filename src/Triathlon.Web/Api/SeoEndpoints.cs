using System.Text;
using System.Xml.Linq;
using Triathlon.Web.Areas.Public;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Api;

/// <summary>
/// The two endpoints a crawler reads before it reads anything else: the sitemap and robots.txt.
/// Kept apart from <see cref="PublicApi"/> because neither belongs to the public site's own
/// culture-prefixed route space — they sit at the bare origin, in front of it.
/// </summary>
public static class SeoEndpoints
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    /// <summary>
    /// The slugs already given in dashboard-managed Pages that carry their own dedicated route
    /// (the home page, join, contact, rules, training) — listed once, by hand, below, so a
    /// published CMS page with the same slug is never emitted a second time as a generic page.
    /// </summary>
    private static readonly HashSet<string> ReservedPageSlugs = new(StringComparer.Ordinal)
    {
        "home", "join", "contact", "rules", "training",
    };

    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/sitemap.xml", BuildSitemapAsync)
            .CacheOutput(policy => policy
                .Expire(OutputCacheSetup.PublicLifetime)
                .Tag(CacheTags.Site, CacheTags.Events, CacheTags.News, CacheTags.Guides));

        // Deliberately not cached: reading a couple of config values per request is cheap, and the
        // staging flag has to flip the whole body the instant the site is promoted or demoted.
        app.MapGet("/robots.txt", BuildRobots);

        return app;
    }

    private static async Task<IResult> BuildSitemapAsync(
        HttpContext http, EventsService events, DocumentsService documents, NewsService news, ContentService content, CancellationToken ct)
    {
        var origin = $"{http.Request.Scheme}://{http.Request.Host}";

        // Every public path worth indexing, relative to the culture segment. Registration and
        // confirmation pages (register/received, events/{slug}/register, events/{slug}/registered),
        // the status pages and the API are excluded by never being added here — not by filtering a
        // bigger list back down.
        var paths = new List<string>
        {
            "", // home
            "events",
            "events/timeline",
            "join",
            "register",
            "training",
            "rules",
            "governance",
            "governance/documents",
            "statistics",
            "news",
            "contact",
        };

        var publishedEvents = await events.AllPublishedAsync(null, ct);
        paths.AddRange(publishedEvents.Select(e => "events/" + e.Slug));

        var guides = await documents.GuidesAsync(ct);
        paths.AddRange(guides.Where(g => g.IsPublished).Select(g => "training/" + g.Slug));

        // NewsService.AllAsync is already "published and its date has arrived" — exactly "live".
        var posts = await news.AllAsync(ct);
        paths.AddRange(posts.Select(p => "news/" + p.Slug));

        var pages = await content.PublishedPageSlugsAsync(ct);
        paths.AddRange(pages.Where(slug => !ReservedPageSlugs.Contains(slug)));

        var urlset = new XElement(SitemapNs + "urlset", new XAttribute(XNamespace.Xmlns + "xhtml", XhtmlNs));

        foreach (var path in paths)
        {
            foreach (var culture in PublicSite.SupportedCultures)
            {
                var url = new XElement(SitemapNs + "url",
                    new XElement(SitemapNs + "loc", origin + PublicCulture.Url(culture, path)));

                foreach (var altCulture in PublicSite.SupportedCultures)
                {
                    url.Add(AlternateLink(altCulture, origin + PublicCulture.Url(altCulture, path)));
                }

                url.Add(AlternateLink("x-default", origin + PublicCulture.Url(PublicSite.DefaultCulture, path)));

                urlset.Add(url);
            }
        }

        // Built with XElement throughout, so every slug and title above is escaped by the writer —
        // nothing here is string-concatenated into the markup.
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), urlset);
        var xml = document.Declaration + "\n" + document.Root;

        return Results.Text(xml, "application/xml", Encoding.UTF8);
    }

    private static XElement AlternateLink(string hreflang, string href) =>
        new(XhtmlNs + "link",
            new XAttribute("rel", "alternate"),
            new XAttribute("hreflang", hreflang),
            new XAttribute("href", href));

    private static IResult BuildRobots(HttpContext http, IConfiguration configuration)
    {
        var staging = configuration.GetValue("Site:Staging", false);

        var body = staging
            ? "User-agent: *\nDisallow: /\n"
            : "User-agent: *\n" +
              "Disallow: /dashboard\n" +
              "Disallow: /api\n" +
              "Allow: /\n" +
              "\n" +
              $"Sitemap: {http.Request.Scheme}://{http.Request.Host}/sitemap.xml\n";

        return Results.Text(body, "text/plain", Encoding.UTF8);
    }
}
