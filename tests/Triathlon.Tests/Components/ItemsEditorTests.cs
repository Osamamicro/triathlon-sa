using Bunit;
using Triathlon.Web.Areas.Dashboard.Components;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Tests.Components;

/// <summary>
/// <see cref="ItemsEditor"/> is the only place a <see cref="BlockType.Table"/> block's rows are
/// edited: one pipe-separated <c>MudTextField</c> per language, split back into
/// <see cref="BlockItem.CellsEn"/>/<see cref="BlockItem.CellsAr"/> on every change. The public
/// <c>_Table.cshtml</c> partial pairs an EN cell with its AR cell by index, so a split that drops
/// empty cells (<c>StringSplitOptions.RemoveEmptyEntries</c>) silently misaligns every cell after
/// the first empty one — this locks the fix (<c>TrimEntries</c> only) down at the component level.
/// </summary>
public sealed class ItemsEditorTests : BunitContext
{
    [Fact]
    public void Table_cell_split_keeps_empty_cells_and_trims_the_rest()
    {
        var items = new List<BlockItem> { new() };

        var cut = Render<ItemsEditor>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.Type, BlockType.Table));

        // Table mode renders the CellsHint caption first, then the EN cells field, then the AR one.
        var enInput = cut.FindAll("input")[0];
        enInput.Change("A ||  C | ");

        Assert.Equal(["A", "", "C", ""], items[0].CellsEn ?? []);
    }

    [Fact]
    public void Table_cell_split_on_the_Arabic_field_also_keeps_empty_cells()
    {
        var items = new List<BlockItem> { new() };

        var cut = Render<ItemsEditor>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.Type, BlockType.Table));

        var arInput = cut.FindAll("input")[1];
        arInput.Change("أ||ب");

        Assert.Equal(["أ", "", "ب"], items[0].CellsAr ?? []);
    }
}
