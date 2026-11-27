using GrepWinUi.Core;

namespace GrepWinUi.Core.Tests;

public class MenuTextTests
{
    [Fact]
    public void Display_NotImplemented_AddsStarInFront()
    {
        Assert.Equal("*Open search query", MenuText.Display("Open search query", isImplemented: false));
    }

    [Fact]
    public void Display_Implemented_KeepsTextUnchanged()
    {
        Assert.Equal("Open search query", MenuText.Display("Open search query", isImplemented: true));
    }

    [Theory]
    [InlineData(@"c:\a&b\x.json", @"c:\a&&b\x.json")]
    [InlineData("a && b", "a &&&& b")]
    [InlineData("plain", "plain")]
    public void Literal_DoublesAmpersand(string text, string expected)
    {
        Assert.Equal(expected, MenuText.Literal(text));
    }
}
