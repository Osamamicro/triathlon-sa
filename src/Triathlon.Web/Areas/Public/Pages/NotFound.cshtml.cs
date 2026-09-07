using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// Public 404. Reached by the status-code re-execute for public paths, and directly.
/// <para>
/// It sets the status code itself so that a direct visit to <c>/en/not-found</c> is honest with
/// crawlers: the re-execute has already set 404 on the response it is rendering into, but a
/// visitor who types the address would otherwise get this page with a 200.
/// </para>
/// </summary>
public sealed class NotFoundModel(IStringLocalizer<Shared> localizer) : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Title"] = localizer["NotFoundTitle"].Value;
    }
}
