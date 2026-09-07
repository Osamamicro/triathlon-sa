using System.Xml.Linq;

namespace Triathlon.Tests.Resources;

/// <summary>
/// Arabic is a first-class language on this site, not a translation afterthought, and a key that
/// exists in one file only fails silently — the missing side falls back to the key name in the
/// middle of a page. So every resx pair in the assembly is compared key for key.
/// </summary>
public sealed class ResxParityTests
{
    [Theory]
    [InlineData("Shared")]
    [InlineData("DashboardStrings")]
    public void English_and_Arabic_carry_the_same_keys(string pair)
    {
        var english = Keys($"{pair}.en.resx");
        var arabic = Keys($"{pair}.ar.resx");

        Assert.Equal(english.Keys.OrderBy(key => key, StringComparer.Ordinal), arabic.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("Shared.en.resx")]
    [InlineData("Shared.ar.resx")]
    [InlineData("DashboardStrings.en.resx")]
    [InlineData("DashboardStrings.ar.resx")]
    public void No_string_is_left_empty(string file)
    {
        var empty = Keys(file).Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key);

        Assert.Empty(empty);
    }

    [Theory]
    [InlineData("Shared")]
    [InlineData("DashboardStrings")]
    public void There_is_something_to_compare(string pair)
    {
        // Guards the test itself: a path that stopped resolving would otherwise pass silently.
        Assert.NotEmpty(Keys($"{pair}.en.resx"));
    }

    private static Dictionary<string, string> Keys(string file)
    {
        var path = Path.Combine(RepositoryRoot(), "src", "Triathlon.Web", "Resources", file);

        return XDocument.Load(path).Root!
            .Elements("data")
            .ToDictionary(
                data => data.Attribute("name")!.Value,
                data => data.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Walks up from the test binaries to the solution file. The resx files are not copied to the
    /// test output — they are embedded in the web assembly under logical names — so this reads the
    /// sources themselves, which is also what a reviewer would diff.
    /// </summary>
    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Triathlon.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not find Triathlon.sln above '{AppContext.BaseDirectory}'.");
    }
}
