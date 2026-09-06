using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// The public home page. Its content is still the prototype's static markup plus the client-side
/// event cards from <c>wwwroot/js/data.js</c>; the dashboard-backed queries arrive with the content
/// weeks, and this is where they will land.
/// </summary>
public sealed class IndexModel(IStringLocalizer<Shared> localizer) : PageModel
{
    public void OnGet() => ViewData["Title"] = localizer["HomeTitle"].Value;
}
