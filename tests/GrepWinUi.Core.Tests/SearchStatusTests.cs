using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Tests of the status bar texts.</summary>
public sealed class SearchStatusTests
{
    private static readonly SearchQuery Query =
        new() { SearchString = "foo", FileNames = "*.cs", FolderName = @"e:\wamp64\www\api_6x" };

    [Theory]
    [InlineData(@"e:\wamp64\www\api_6x", "api_6x")]
    [InlineData(@"e:\wamp64\www\api_6x\", "api_6x")]
    [InlineData(@"e:\", @"e:\")]
    [InlineData("", "")]
    public void LastFolderName_LastPart(string folder, string expected) =>
        Assert.Equal(expected, SearchStatus.LastFolderName(folder));

    [Fact]
    public void TimeText_HoursMinutesSeconds()
    {
        Assert.Equal("00:00:07", SearchStatus.TimeText(TimeSpan.FromSeconds(7.9)));
        Assert.Equal("01:02:03", SearchStatus.TimeText(new TimeSpan(1, 2, 3)));
        Assert.Equal("25:00:00", SearchStatus.TimeText(TimeSpan.FromHours(25)));
    }

    [Fact]
    public void Running_NoProgressYet_SearchFolderAndZeroFiles()
    {
        SearchStatus status = SearchStatus.Running(Query, progress: null, TimeSpan.Zero, showFileNames: true);

        Assert.Equal(new SearchStatus("Searching 'foo'", "", "api_6x", "0 / ?", "00:00:00", IsRunning: true), status);
    }

    [Fact]
    public void Running_WithProgress_CurrentFileAndItsFolder()
    {
        SearchProgress progress = new(12, 3, 2, @"e:\wamp64\www\api_6x\src\a.cs");

        SearchStatus status = SearchStatus.Running(Query, progress, TimeSpan.FromSeconds(7), showFileNames: true);

        Assert.Equal(@"e:\wamp64\www\api_6x\src\a.cs", status.Info);
        Assert.Equal("src", status.Folder);
        Assert.Equal("12 / ?", status.Files);
        Assert.Equal("00:00:07", status.Time);
    }

    [Fact]
    public void Running_FileNamesOff_EmptyInfo()
    {
        SearchProgress progress = new(12, 3, 2, @"e:\wamp64\www\api_6x\src\a.cs");

        Assert.Equal("", SearchStatus.Running(Query, progress, TimeSpan.Zero, showFileNames: false).Info);
    }

    [Fact]
    public void Running_CountFilesFirst_NoProgressYet_Counting()
    {
        SearchQuery query = Query.Copy();
        query.CountFilesFirst = true;

        SearchStatus status = SearchStatus.Running(query, progress: null, TimeSpan.Zero, showFileNames: false);

        Assert.Equal("Counting files for 'foo'", status.Title);
        Assert.Equal("0 / ?", status.Files);
        Assert.Null(status.Percent);
    }

    [Fact]
    public void Running_Counting_FilesCountedSoFar()
    {
        SearchProgress progress = new(0, 3, 0, @"e:\wamp64\www\api_6x\src\a.cs", TotalFiles: 345, Counting: true);

        SearchStatus status = SearchStatus.Running(Query, progress, TimeSpan.Zero, showFileNames: false);

        Assert.Equal("Counting files for 'foo'", status.Title);
        Assert.Equal("345 / ?", status.Files);
        Assert.Null(status.Percent);
    }

    [Fact]
    public void Running_TotalKnown_ReadOfAllAndPercent()
    {
        SearchProgress progress = new(12, 3, 2, @"e:\wamp64\www\api_6x\src\a.cs", TotalFiles: 57);

        SearchStatus status = SearchStatus.Running(Query, progress, TimeSpan.Zero, showFileNames: false);

        Assert.Equal("Searching 'foo'", status.Title);
        Assert.Equal("12 / 57", status.Files);
        Assert.Equal(21, status.Percent);
    }

    [Theory]
    [InlineData(0, 57, 0)]
    [InlineData(57, 57, 100)]
    [InlineData(0, 0, 100)]
    [InlineData(60, 57, 100)]
    public void PercentOf_Clamped(int done, int all, int expected) =>
        Assert.Equal(expected, SearchStatus.PercentOf(done, all));

    [Fact]
    public void Done_SummaryAndAllFiles()
    {
        SearchStatus status = SearchStatus.Done(Result(cancelled: false));

        Assert.Equal(new SearchStatus("Search done", "154 matches in 2 files from 57 files searched",
            "api_6x", "57 / 57", "00:00:04", IsRunning: false), status);
    }

    [Fact]
    public void Done_Cancelled_AllFilesUnknown()
    {
        SearchStatus status = SearchStatus.Done(Result(cancelled: true));

        Assert.Equal("Search cancelled", status.Title);
        Assert.Equal("57 / ?", status.Files);
    }

    [Fact]
    public void Failed_MessageInInfo()
    {
        SearchStatus status = SearchStatus.Failed(Query, "boom", TimeSpan.FromSeconds(1));

        Assert.Equal(new SearchStatus("Search failed", "boom", "api_6x", "", "00:00:01", IsRunning: false), status);
    }

    private static SearchResult Result(bool cancelled) => new(
        Query,
        [new FileResult(@"e:\a.cs", []), new FileResult(@"e:\b.cs", [])],
        FilesSearched: 57, FoldersSearched: 4, MatchCount: 154, Duration: TimeSpan.FromSeconds(4.2),
        Cancelled: cancelled, Problems: []);
}
