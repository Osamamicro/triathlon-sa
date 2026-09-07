using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Triathlon.Tests.Web;

/// <summary>Signing into the dashboard the way a browser does: fetch the form, carry its hidden fields back.</summary>
public static partial class DashboardClient
{
    [GeneratedRegex("<input[^>]*type=\"hidden\"[^>]*>")]
    private static partial Regex HiddenInput();

    /// <summary>A client over https (the identity cookie is Secure-only) that is already signed in as <paramref name="email"/>.</summary>
    public static async Task<HttpClient> CreateSignedInClientAsync(WebAppFixture app, bool allowAutoRedirect = false, string? email = null, string? password = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect,
            BaseAddress = WebAppFixture.HttpsBaseAddress,
        });
        await SignInAsync(client, email, password);
        return client;
    }

    public static async Task SignInAsync(HttpClient client, string? email = null, string? password = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        var loginPage = await client.GetStringAsync("/dashboard/login");
        var form = HiddenFields(loginPage);
        Assert.Contains("__RequestVerificationToken", form.Keys);

        form["Input.Email"] = email ?? WebAppFixture.AdminEmail;
        form["Input.Password"] = password ?? WebAppFixture.AdminPassword;

        using var signIn = await client.PostAsync("/dashboard/login", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Found, signIn.StatusCode);
        Assert.DoesNotContain("/dashboard/login", signIn.Headers.Location!.ToString());
    }

    /// <summary>Every hidden input on a page — the antiforgery token and Blazor's form handler.</summary>
    public static Dictionary<string, string> HiddenFields(string html)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var input in HiddenInput().Matches(html).Cast<Match>())
        {
            var name = Regex.Match(input.Value, "name=\"([^\"]+)\"");
            if (!name.Success) continue;
            var value = Regex.Match(input.Value, "value=\"([^\"]*)\"");
            fields[WebUtility.HtmlDecode(name.Groups[1].Value)] = WebUtility.HtmlDecode(value.Success ? value.Groups[1].Value : string.Empty);
        }
        return fields;
    }
}
