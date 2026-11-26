namespace GrepWinUi.Core.Tests;

public sealed class FocusOrderTests
{
    [Fact]
    public void FirstThenRest_MainInputsFirstRestInNaturalOrder()
    {
        string[] natural = ["search", "searchOptions", "files", "fileOptions", "folder", "folderOptions", "buttons"];

        IReadOnlyList<string> order = FocusOrder.FirstThenRest(natural, ["search", "files", "folder"]);

        Assert.Equal(["search", "files", "folder", "searchOptions", "fileOptions", "folderOptions", "buttons"], order);
    }

    [Theory]
    [InlineData(0, true, 1)]
    [InlineData(4, true, 0)]
    [InlineData(1, false, 0)]
    [InlineData(0, false, 4)]
    [InlineData(-1, true, 0)]
    [InlineData(-1, false, 4)]
    public void NextIndex_WrapsAround(int current, bool forward, int expected)
    {
        Assert.Equal(expected, FocusOrder.NextIndex(5, current, forward));
    }
}
