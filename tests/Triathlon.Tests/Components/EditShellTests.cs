using Bunit;
using Triathlon.Web.Areas.Dashboard.Components;

namespace Triathlon.Tests.Components;

/// <summary>
/// The one edit-screen frame every content type reuses. A refused save's <c>FieldErrors</c> must
/// stay readable even when the screen that threw them forgot to anchor a field's message on its own
/// input — see the Week 4 final-review fix wave: an over-length KPI Suffix or an unanchored Events
/// Categories/Results refusal used to show nothing but the generic "fix the fields below" banner.
/// </summary>
public sealed class EditShellTests : BunitContext
{
    [Fact]
    public void A_field_error_with_no_matching_input_still_renders_its_message()
    {
        var fieldErrors = new Dictionary<string, string>(StringComparer.Ordinal) { ["Nope"] = "msg" };

        var cut = Render<EditShell>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.BackHref, "dashboard/test")
            .Add(p => p.ActivityEntity, "Test")
            .Add(p => p.FieldErrors, fieldErrors));

        Assert.Contains("msg", cut.Markup, StringComparison.Ordinal);
    }
}
