using Microsoft.AspNetCore.Mvc.RazorPages;
using Triathlon.Web.Api;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// Where an applicant lands after the registration endpoint has written their record — the "get" of
/// post/redirect/get, so a refresh cannot apply twice. Uncached: it is one applicant's own
/// acknowledgement, addressed to them by name.
/// </summary>
public sealed class RegisterReceivedModel : PageModel
{
    /// <summary>
    /// The name the applicant typed, carried through TempData rather than the redirect's query
    /// string (see <see cref="PublicApi.ConfirmationNameKey"/>) and re-encoded by Razor.
    /// </summary>
    public string? Name { get; private set; }

    public void OnGet()
    {
        Name = TempData[PublicApi.ConfirmationNameKey] as string;

        ViewData["Title"] = PublicText.Bi("Application received", "تم استلام الطلب");
    }
}
