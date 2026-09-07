using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Triathlon.Web.Areas.Public;

/// <summary>
/// Round-trips a rejected public form through TempData: what the visitor typed and which fields
/// failed, so a post → redirect → get flow can hand the form back pre-filled with only the failing
/// fields flagged, instead of a blank page and one generic sentence.
/// <para>
/// TempData here is the cookie-backed <c>CookieTempDataProvider</c> that <c>AddRazorPages()</c>
/// registers by default. A Razor Page reads and writes it through <c>PageModel.TempData</c> for
/// free; a minimal API endpoint has no MVC result filter to save it after the delegate returns, so
/// it must resolve <see cref="ITempDataDictionaryFactory"/> itself and call
/// <see cref="ITempDataDictionary.Save"/> before the response is sent — confirmed working here
/// because the cookie is appended synchronously against <c>HttpContext.Response</c>, which happens
/// before the minimal API result (the redirect) writes the status line.
/// </para>
/// Kept deliberately small so a later form (athlete registration) can reuse the same two calls.
/// </summary>
public static class FormRoundTrip
{
    private const string ValuesKey = "FormRoundTrip.Values";
    private const string InvalidKey = "FormRoundTrip.Invalid";

    /// <summary>Stores the posted values and the names of the fields that failed validation.</summary>
    public static void Store(ITempDataDictionary tempData, IDictionary<string, string?> values, IEnumerable<string> invalid)
    {
        ArgumentNullException.ThrowIfNull(tempData);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(invalid);

        tempData[ValuesKey] = JsonSerializer.Serialize(values);
        tempData[InvalidKey] = string.Join(',', invalid);
    }

    /// <summary>
    /// Reads back what <see cref="Store"/> saved, if anything is there — TempData is one-time-read,
    /// so calling this consumes it. Never throws on missing or malformed data; a visitor who lands
    /// on the form without a round trip just gets empty results.
    /// </summary>
    public static (IReadOnlyDictionary<string, string> Values, IReadOnlySet<string> Invalid) TryRead(ITempDataDictionary tempData)
    {
        ArgumentNullException.ThrowIfNull(tempData);

        var valuesJson = tempData[ValuesKey] as string;
        var invalidCsv = tempData[InvalidKey] as string;

        Dictionary<string, string> values = [];
        if (!string.IsNullOrEmpty(valuesJson))
        {
            try
            {
                values = JsonSerializer.Deserialize<Dictionary<string, string>>(valuesJson) ?? [];
            }
            catch (JsonException)
            {
                values = [];
            }
        }

        var invalid = string.IsNullOrEmpty(invalidCsv)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(invalidCsv.Split(',', StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);

        return (values, invalid);
    }
}
