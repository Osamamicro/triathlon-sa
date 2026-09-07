using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Web;

[Collection(WebAppCollection.Name)]
public sealed class DownloadTests(WebAppFixture app)
{
    [Fact]
    public async Task Document_download_redirects_to_the_file_and_is_never_cached()
    {
        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
            id = (await scope.ServiceProvider.GetRequiredService<DocumentsService>().QueryAsync(null, null, CancellationToken.None))[0].Id;

        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var first = await client.GetAsync($"/documents/{id}/download");
        using var second = await client.GetAsync($"/documents/{id}/download");

        Assert.Equal(HttpStatusCode.Found, first.StatusCode);
        Assert.StartsWith("/docs/", first.Headers.Location!.ToString(), StringComparison.Ordinal);
        Assert.False(second.Headers.Contains("Age"));

        using var file = await client.GetAsync(first.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("application/pdf", file.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Unknown_document_is_404()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync($"/documents/{Guid.NewGuid()}/download");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
