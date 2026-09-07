using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Triathlon.Tests.Web;

/// <summary>
/// The dashboard has no "culture" URL segment (see PublicSite.AddPublicSite), so its language comes
/// from a cookie instead. These cover the cookie deciding the dashboard's language, the endpoint that
/// sets it while only ever redirecting locally, and that the public site's URL-driven culture is
/// never overridden by a stale dashboard cookie.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class DashboardCultureTests(WebAppFixture app)
{
    [Fact]
    public async Task Login_is_english_by_default_and_arabic_with_the_culture_cookie()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = WebAppFixture.HttpsBaseAddress });
        var english = await client.GetStringAsync("/dashboard/login");
        Assert.Contains("<html lang=\"en\" dir=\"ltr\"", english, StringComparison.Ordinal);
        Assert.Contains(">Sign in<", english, StringComparison.Ordinal);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard/login");
        request.Headers.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");
        using var response = await client.SendAsync(request);
        var arabic = await response.Content.ReadAsStringAsync();
        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", arabic, StringComparison.Ordinal);
        Assert.Contains(">تسجيل الدخول<", arabic, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Culture_endpoint_sets_the_cookie_and_redirects_locally_only()
    {
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false, BaseAddress = WebAppFixture.HttpsBaseAddress });
        using var ok = await client.GetAsync("/dashboard/culture/ar?returnUrl=/dashboard/login");
        Assert.Equal(HttpStatusCode.Found, ok.StatusCode);
        Assert.Equal("/dashboard/login", ok.Headers.Location!.ToString());
        Assert.Contains(ok.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal) && c.Contains("uic%3Dar", StringComparison.Ordinal));

        using var open = await client.GetAsync("/dashboard/culture/ar?returnUrl=https://evil.example/");
        Assert.Equal("/dashboard", open.Headers.Location!.ToString());
        using var bad = await client.GetAsync("/dashboard/culture/fr");
        Assert.Equal(HttpStatusCode.NotFound, bad.StatusCode);
    }

    [Fact]
    public async Task Public_url_culture_beats_the_cookie()
    {
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/en");
        request.Headers.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");
        using var response = await client.SendAsync(request);
        Assert.Contains("<html lang=\"en\" dir=\"ltr\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
