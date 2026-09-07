using Microsoft.AspNetCore.OutputCaching;

namespace Triathlon.Web.Services;

/// <summary>
/// The vocabulary that links a cached public page to the content that produced it.
/// <para>
/// Every cached page is tagged with the content it renders; every dashboard save evicts the tags it
/// touched. That is the whole contract: an editor presses Publish and the affected pages go stale
/// immediately, while everything else keeps serving from cache.
/// </para>
/// </summary>
public static class CacheTags
{
    /// <summary>
    /// Carried by every public page; evicting it drops the whole public cache — used when the
    /// navigation or a site setting changes, which is content no single page tag covers.
    /// </summary>
    public const string Site = "site";

    public const string Home = "home";
    public const string Events = "events";
    public const string Stats = "stats";
    public const string News = "news";
    public const string Documents = "documents";
    public const string Governance = "governance";
    public const string Rules = "rules";
    public const string Guides = "guides";

    /// <summary>
    /// The affiliated club list. It is its own tag because the clubs block appears on pages that
    /// otherwise have nothing to do with each other — the join page and the training page today.
    /// Committees have no tag of their own: they only ever render under <see cref="Governance"/>.
    /// </summary>
    public const string Clubs = "clubs";

    /// <summary>Tag for one CMS page, so editing it does not evict every other page.</summary>
    public static string Page(string slug) => $"page:{slug}";

    /// <summary>Tag for one event's own page.</summary>
    public static string Event(string slug) => $"event:{slug}";
}

/// <summary>Convenience over <see cref="IOutputCacheStore"/> for the common multi-tag eviction.</summary>
public static class OutputCacheStoreExtensions
{
    /// <summary>
    /// Evicts every cached response carrying any of <paramref name="tags"/>. Call it after a
    /// content change has been committed, never before — eviction is not transactional.
    /// </summary>
    public static async Task EvictAsync(this IOutputCacheStore store, CancellationToken ct, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(tags);

        foreach (var tag in tags)
        {
            await store.EvictByTagAsync(tag, ct);
        }
    }
}
