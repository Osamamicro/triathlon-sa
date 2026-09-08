using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;

namespace Triathlon.Tests.Services;

/// <summary>
/// A second application under the same email must not become a second athlete or a second mail —
/// the applicant almost certainly resubmitted (a slow connection, a double click, a bookmarked
/// form), and the desk should see one pending record, not two.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class AthleteDedupeTests(WebAppFixture app)
{
    [Fact]
    public async Task Registering_twice_with_the_same_email_keeps_one_athlete_and_sends_one_mail()
    {
        using var client = app.CreateClient();
        var email = $"dedupe-{Guid.NewGuid():N}@example.test";

        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());
        using var first = await Forms.PostFormAsync(client, "/en/register", "/api/register", new()
        {
            ["culture"] = "en", ["fullName"] = "Dedupe One", ["email"] = email, ["dateOfBirth"] = "1995-04-12",
            ["city"] = "riyadh", ["category"] = "Age Group", ["club"] = "", ["event"] = "", ["declaration"] = "on",
        });
        Assert.Contains("/en/register/received", first.RequestMessage!.RequestUri!.PathAndQuery, StringComparison.Ordinal);

        client.DefaultRequestHeaders.Remove(TestClientIp.Header);
        client.DefaultRequestHeaders.Add(TestClientIp.Header, TestClientIp.Unique());
        using var second = await Forms.PostFormAsync(client, "/en/register", "/api/register", new()
        {
            ["culture"] = "en", ["fullName"] = "Dedupe Two", ["email"] = email, ["dateOfBirth"] = "1996-05-13",
            ["city"] = "jeddah", ["category"] = "Elite", ["club"] = "", ["event"] = "", ["declaration"] = "on",
        });
        Assert.Contains("/en/register/received", second.RequestMessage!.RequestUri!.PathAndQuery, StringComparison.Ordinal);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Athletes.CountAsync(a => a.Email == email));
        Assert.Equal(1, app.Emails.Count(m => m.To == email));
    }
}
