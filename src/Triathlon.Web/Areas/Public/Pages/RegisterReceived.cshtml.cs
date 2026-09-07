using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Triathlon.Web.Areas.Public.Pages;

/// <summary>
/// Where an applicant lands after the registration endpoint has written their record — the "get" of
/// post/redirect/get, so a refresh cannot apply twice. Uncached: it is one applicant's own
/// acknowledgement, addressed to them by name.
/// </summary>
public sealed class RegisterReceivedModel : PageModel
{
    /// <summary>The name the applicant typed, carried on the redirect and re-encoded by Razor.</summary>
    public string? Name { get; private set; }

    public void OnGet(string? name)
    {
        Name = name;

        ViewData["Title"] = PublicText.Bi("Application received", "تم استلام الطلب");
    }
}
