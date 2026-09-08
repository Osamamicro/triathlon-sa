using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triathlon.Tests.Web;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Documents;
using Triathlon.Web.Services;

namespace Triathlon.Tests.Services;

/// <summary>
/// The write half of the library: a document save is one activity-log row and one eviction of the
/// documents/governance tags, a guide's chapters are sanitised and follow ADR 0001 on delete/restore
/// like every other owned child, and every stored file path goes through <see cref="ContentGuard"/>.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class LibraryWriteTests(WebAppFixture app)
{
    [Fact]
    public async Task Saving_a_document_evicts_the_library_and_logs_a_create()
    {
        using var client = app.CreateClient();
        _ = await client.GetStringAsync("/en/governance/documents");
        Assert.True((await client.GetAsync("/en/governance/documents")).Headers.Contains("Age"));

        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var documents = scope.ServiceProvider.GetRequiredService<DocumentsService>();
            var doc = await documents.SaveDocumentAsync(
                new DocumentInput(null, "Write test", "اختبار الكتابة", DocumentCategory.Governance, 2026, "/docs/write-test.pdf", 1024, true, 1),
                CancellationToken.None);
            id = doc.Id;
        }

        try
        {
            using var after = await client.GetAsync("/en/governance/documents");
            Assert.False(after.Headers.Contains("Age"), "documents tag was not evicted by the save");

            await using var scope2 = app.Services.CreateAsyncScope();
            var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var actions = await db.ActivityLogs.Where(l => l.Entity == "Document" && l.EntityId == id.ToString()).Select(l => l.Action).ToListAsync();
            Assert.Equal(["create"], actions);
        }
        finally
        {
            // A published document is counted by DocumentsServiceTests' seeded-data assertions and
            // picked as "the first document" by DownloadTests — this test's row must not outlive it.
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DocumentsService>().DeleteDocumentAsync(id, CancellationToken.None);
        }
    }

    [Fact]
    public async Task A_document_file_path_outside_the_site_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentsService>();

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() => documents.SaveDocumentAsync(
            new DocumentInput(null, "Bad file", "ملف سيء", DocumentCategory.Governance, 2026, "https://evil.example/x.pdf", 10, true, 1),
            CancellationToken.None));
        Assert.Equal("FilePath", ex.Field);
    }

    [Fact]
    public async Task A_guide_save_sanitises_chapters_and_delete_restore_follows_ADR_0001()
    {
        var slug = "write-" + Guid.NewGuid().ToString("N")[..8];
        using var client = app.CreateClient();

        var chapters = new[]
        {
            new ChapterInput(null, "Swim", "سباحة", "<p>Swim<script>alert(1)</script></p>", "<p>سباحة</p>"),
            new ChapterInput(null, "Bike", "دراجة", "<p>Bike</p>", "<p>دراجة</p>"),
        };

        Guid id;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var documents = scope.ServiceProvider.GetRequiredService<DocumentsService>();
            var guide = await documents.SaveGuideAsync(
                new GuideInput(null, slug, "Write test", "اختبار الكتابة", "Summary", "ملخص", "Beginner", "مبتدئ", null, null, true, 1, chapters),
                CancellationToken.None);
            id = guide.Id;
        }

        var page = await client.GetStringAsync("/en/training/" + slug);
        Assert.Contains("<p>Swim</p>", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>alert", page, StringComparison.Ordinal);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DocumentsService>().DeleteGuideAsync(id, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Empty(await db.TrainingGuideChapters.Where(c => c.TrainingGuideId == id).ToListAsync());
            var allChapters = await db.TrainingGuideChapters.IgnoreQueryFilters().Where(c => c.TrainingGuideId == id).ToListAsync();
            Assert.Equal(2, allChapters.Count);
            Assert.All(allChapters, c => Assert.NotNull(c.DeletedAt));
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DocumentsService>().RestoreGuideAsync(id, CancellationToken.None);
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.TrainingGuideChapters.Where(c => c.TrainingGuideId == id).CountAsync());
        }

        // A live, published guide is counted by DocumentsServiceTests' seeded-data assertions —
        // deleted again once the restore itself has been proven, so this test's row does not
        // outlive it.
        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DocumentsService>().DeleteGuideAsync(id, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Publishing_a_guide_with_no_file_and_no_chapters_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentsService>();
        var slug = "empty-" + Guid.NewGuid().ToString("N")[..8];

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() => documents.SaveGuideAsync(
            new GuideInput(null, slug, "Empty", "فارغ", "Summary", "ملخص", "Beginner", "مبتدئ", null, null, true, 1, []),
            CancellationToken.None));
        Assert.Equal("IsPublished", ex.Field);
    }

    [Fact]
    public async Task Publishing_a_guide_with_a_whitespace_file_path_and_no_chapters_is_refused()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var documents = scope.ServiceProvider.GetRequiredService<DocumentsService>();
        var slug = "blank-path-" + Guid.NewGuid().ToString("N")[..8];

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() => documents.SaveGuideAsync(
            new GuideInput(null, slug, "Blank path", "مسار فارغ", "Summary", "ملخص", "Beginner", "مبتدئ", " ", 10, true, 1, []),
            CancellationToken.None));
        Assert.Equal("IsPublished", ex.Field);
    }
}
