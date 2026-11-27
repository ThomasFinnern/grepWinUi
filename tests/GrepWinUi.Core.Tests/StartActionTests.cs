using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.Core.Tests;

/// <summary>What happens on start with the active result form ("On start of program" settings).</summary>
public sealed class StartActionTests
{
    [Theory]
    // repeat, openForm, reopened, search string → action
    [InlineData(false, false, false, "foo", StartAction.OpenDialog)]
    [InlineData(false, true, false, "foo", StartAction.OpenDialog)]
    [InlineData(false, false, true, "foo", StartAction.None)]
    [InlineData(false, true, true, "foo", StartAction.OpenDialog)]
    [InlineData(true, false, false, "foo", StartAction.Search)]
    [InlineData(true, true, false, "foo", StartAction.Search)]
    [InlineData(true, false, true, "foo", StartAction.Search)]
    [InlineData(true, true, true, "foo", StartAction.Search)]
    [InlineData(true, false, false, "", StartAction.OpenDialog)]
    [InlineData(true, false, true, "", StartAction.None)]
    [InlineData(true, true, true, "", StartAction.OpenDialog)]
    public void Decide(bool repeat, bool openForm, bool reopened, string searchString, StartAction expected)
    {
        AppConfig config = new() { RepeatLastSearchOnStart = repeat, OpenSearchFormOnStart = openForm };

        Assert.Equal(expected, StartActions.Decide(config, reopened, searchString));
    }
}
