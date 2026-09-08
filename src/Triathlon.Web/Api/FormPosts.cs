using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Triathlon.Web.Areas.Public;

namespace Triathlon.Web.Api;

/// <summary>
/// What both public form-posting endpoints in <see cref="PublicApi"/> do identically: turn a blank
/// optional field into "not typed", turn a C# property name into the lower-camel-case field name the
/// HTML input, the TempData round trip and the "field invalid" CSS hook all key off, and save a
/// rejected post's values and failing fields to TempData by hand.
/// </summary>
public static class FormPosts
{
    /// <summary>A browser posts "" for an optional field left blank or a select left on its blank option; both mean "not chosen".</summary>
    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// <see cref="ValidationResult.MemberNames"/> carries the C# property name ("FullName"); the
    /// form field, the TempData round trip and the "field invalid" CSS hook all key off the
    /// lower-camel-case name the HTML input uses instead ("fullName").
    /// </summary>
    public static string ToFieldName(string memberName) =>
        memberName.Length == 0 ? memberName : char.ToLowerInvariant(memberName[0]) + memberName[1..];

    /// <summary>
    /// Stores what the visitor posted (keyed by C# property name — converted to field names here)
    /// and which fields failed validation, then saves TempData. A minimal API delegate never runs
    /// through MVC's result filters, which is what saves TempData for a Razor Page automatically —
    /// so this is called by hand, before the caller's <c>IResult</c> sets the redirect's status line
    /// and Location header. <see cref="ITempDataDictionary.Save"/> appends the cookie onto
    /// <see cref="HttpContext.Response"/> synchronously, which is what lets both land on the same
    /// 302 response.
    /// </summary>
    public static void RoundTrip(
        ITempDataDictionaryFactory tempDataFactory,
        HttpContext httpContext,
        IEnumerable<ValidationResult> results,
        IDictionary<string, string?> posted)
    {
        ArgumentNullException.ThrowIfNull(tempDataFactory);
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(posted);

        var invalidFields = results
            .SelectMany(r => r.MemberNames)
            .Select(ToFieldName)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var fields = posted.ToDictionary(kv => ToFieldName(kv.Key), kv => kv.Value);

        var tempData = tempDataFactory.GetTempData(httpContext);
        FormRoundTrip.Store(tempData, fields, invalidFields);
        tempData.Save();
    }
}
