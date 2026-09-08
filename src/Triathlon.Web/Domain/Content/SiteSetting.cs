using Triathlon.Web.Domain.Common;

namespace Triathlon.Web.Domain.Content;

/// <summary>One bilingual site-wide value the layout renders — contact details, the footer blurb. Keyed, never deleted.</summary>
public sealed class SiteSetting : BaseEntity
{
    public required string Key { get; set; }
    public string ValueEn { get; set; } = "";
    public string ValueAr { get; set; } = "";
}

/// <summary>The keys the layout reads. Adding one here means adding it to the structural seed and to the footer.</summary>
public static class SettingKeys
{
    public const string ContactEmail = "contact.email";
    public const string ContactWebsite = "contact.website";
    public const string ContactX = "contact.x";
    public const string FooterBlurb = "footer.blurb";

    public static readonly string[] All = [ContactEmail, ContactWebsite, ContactX, FooterBlurb];
}
