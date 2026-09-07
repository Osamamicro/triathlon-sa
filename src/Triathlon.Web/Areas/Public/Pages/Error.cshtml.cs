using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Net.Http.Headers;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// Public 500. Reached by the exception handler re-executing the failed public request.
/// <para>
/// Never stored: the request that produced it failed for a reason that may already be fixed, and a
/// proxy holding on to this page would keep serving the outage after the site recovered.
/// </para>
/// </summary>
public sealed class ErrorModel(IStringLocalizer<Shared> localizer) : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        Response.Headers[HeaderNames.CacheControl] = "no-store";
        ViewData["Title"] = localizer["ErrorTitle"].Value;
    }
}
