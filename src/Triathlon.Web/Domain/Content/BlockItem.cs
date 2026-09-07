using System.Text.Json;
using System.Text.Json.Serialization;

namespace Triathlon.Web.Domain.Content;

/// <summary>
/// One repeating entry inside a <see cref="PageBlock"/> — a step, a card, a table row, a question.
/// Blocks differ in which fields they use, so every field is optional and the block's partial
/// decides what it reads; the whole list is stored as JSON text in
/// <see cref="PageBlock.ItemsJson"/> rather than as a table, because nothing ever queries it.
/// </summary>
/// <param name="Color">A discipline colour token: <c>swim</c>, <c>bike</c> or <c>run</c>.</param>
public sealed record BlockItem(
    string? EyebrowEn = null, string? EyebrowAr = null,
    string? TitleEn = null, string? TitleAr = null,
    string? BodyEn = null, string? BodyAr = null,
    string? Href = null, string? Color = null,
    string[]? CellsEn = null, string[]? CellsAr = null)
{
    public static IReadOnlyList<BlockItem> Parse(string json) =>
        JsonSerializer.Deserialize<List<BlockItem>>(json, JsonOptions) ?? [];

    public static string Serialize(IEnumerable<BlockItem> items) => JsonSerializer.Serialize(items, JsonOptions);

    /// <summary>
    /// Web defaults for camel-cased names, and nulls dropped so an item that uses three of the ten
    /// fields stores three of them — this JSON is edited by hand in the dashboard, not only by code.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
}
