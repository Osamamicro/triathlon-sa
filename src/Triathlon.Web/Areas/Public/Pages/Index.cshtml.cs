using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using Triathlon.Web.Infrastructure;
using Triathlon.Web.Services;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// The public home page. Its content is still the prototype's static markup plus the client-side
/// event cards from <c>wwwroot/js/data.js</c>; the dashboard-backed queries arrive with the content
/// weeks, and this is where they will land.
/// <para>
/// Cached under the <c>Home</c> tag, so publishing from the dashboard drops it immediately rather
/// than leaving the site a minute behind the editor who just pressed the button.
/// </para>
/// </summary>
[OutputCache(PolicyName = OutputCacheSetup.PublicPolicy, Tags = [CacheTags.Home])]
public sealed class IndexModel(IStringLocalizer<Shared> localizer) : PageModel
{
    public void OnGet() => ViewData["Title"] = localizer["HomeTitle"].Value;
}
