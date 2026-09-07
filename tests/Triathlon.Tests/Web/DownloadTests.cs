using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task A_document_whose_stored_path_is_not_a_site_file_is_404_not_a_redirect()
    {
        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Triathlon.Web.Data.AppDbContext>();
            var doc = new Triathlon.Web.Domain.Documents.Document
            {
                TitleEn = "Tampered", TitleAr = "معدل", Category = Triathlon.Web.Domain.Documents.DocumentCategory.Governance,
                Year = 2026, FilePath = "https://evil.example/x.pdf", IsPublished = true, SortOrder = 99,
            };
            db.Documents.Add(doc);
            await db.SaveChangesAsync();
            id = doc.Id;
        }

        try
        {
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var response = await client.GetAsync($"/documents/{id}/download");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<Triathlon.Web.Data.AppDbContext>();
            db.Documents.Remove(await db.Documents.SingleAsync(d => d.Id == id));
            await db.SaveChangesAsync();
        }
    }
}
