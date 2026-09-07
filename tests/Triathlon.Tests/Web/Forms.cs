using System.Text.RegularExpressions;

namespace Triathlon.Tests.Web;

/// <summary>Posting a public form the way a browser does: fetch the page, carry its token back.</summary>
public static partial class Forms
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    /// <summary>GETs the page that renders the form (cookie + token), then POSTs the fields with that token.</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string formPath, string postPath, Dictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(fields);

        var page = await client.GetStringAsync(formPath);
        var token = TokenPattern().Match(page).Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), $"No antiforgery token on {formPath}");

        fields["__RequestVerificationToken"] = token;
        return await client.PostAsync(postPath, new FormUrlEncodedContent(fields));
    }
}
