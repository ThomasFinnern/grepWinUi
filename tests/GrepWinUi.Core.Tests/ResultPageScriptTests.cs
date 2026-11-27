using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Click on a file in the file list → matching lines pane scrolls to that file.</summary>
public sealed class ResultPageScriptTests
{
    [Theory]
    [InlineData("""{"kind":"click","file":0}""", 0)]
    [InlineData("""{"kind":"click","file":12}""", 12)]
    [InlineData("""{"file":3,"kind":"click"}""", 3)]
    public void ParseFileClick_ClickMessage_Index(string json, int expected)
    {
        Assert.Equal(expected, ResultPageScript.ParseFileClick(json));
    }

    [Theory]
    [InlineData("""{"kind":"open","file":1}""")]
    [InlineData("""{"kind":"click","file":-1}""")]
    [InlineData("""{"kind":"click","file":1.5}""")]
    [InlineData("""{"kind":"click","file":"1"}""")]
    [InlineData("""{"kind":"click"}""")]
    [InlineData("\"click\"")]
    [InlineData("not json")]
    [InlineData("")]
    public void ParseFileClick_OtherOrBroken_Null(string json)
    {
        Assert.Null(ResultPageScript.ParseFileClick(json));
    }

    [Fact]
    public void ScrollToFile_UsesFileId()
    {
        Assert.Equal("file-4", ResultPageScript.FileId(4));
        Assert.Contains("getElementById('file-4')", ResultPageScript.ScrollToFile(4));
        Assert.Contains("scrollIntoView", ResultPageScript.ScrollToFile(4));
    }

    [Fact]
    public void FileList_RowsHaveIndexAndPageHasScript()
    {
        string html = ResultPageHtml.FileList(Result(), "", "white");

        Assert.Contains("""<tr data-file="0"><td class="FileName"><div title="a.cs">""", html);
        Assert.Contains("""<tr data-file="1"><td class="FileName"><div title="b.cs">""", html);
        Assert.Contains("<script>\n" + ResultPageScript.FileList + "\n</script></body>", html);
    }

    [Fact]
    public void FileList_NoFiles_NoScript()
    {
        string html = ResultPageHtml.FileList(Result() with { Files = [] }, "", "white");

        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void MatchingLines_FileTitlesHaveIds()
    {
        string html = ResultPageHtml.MatchingLines(Result(), new ResultViewOptions(), "", "white");

        Assert.Contains("""<div class="FileNameTitle" id="file-0">c:\src\a.cs</div>""", html);
        Assert.Contains("""<div class="FileNameTitle" id="file-1">c:\src\b.cs</div>""", html);
    }

    [Fact]
    public void MatchingLines_FileNamesOff_EmptyAnchorPerFile()
    {
        string html = ResultPageHtml.MatchingLines(Result(), new ResultViewOptions { ShowFileNames = false }, "", "white");

        Assert.Contains("""<span id="file-0"></span>""", html);
        Assert.Contains("""<span id="file-1"></span>""", html);
        Assert.DoesNotContain("FileNameTitle", html);
    }

    [Fact]
    public void FileListCss_HasHighlightRule()
    {
        Assert.Contains("tr.highlight td {", ResultCss.FileList);
    }

    private static SearchResult Result() => new(
        new SearchQuery { SearchString = "foo", FileNames = "*.cs", FolderName = @"c:\src" },
        [
            new FileResult(@"c:\src\a.cs", [new LineMatch(1, "foo", [new Hit(0, 3)])]),
            new FileResult(@"c:\src\b.cs", [new LineMatch(2, "foo", [new Hit(0, 3)])]),
        ],
        FilesSearched: 2, FoldersSearched: 1, MatchCount: 2, Duration: TimeSpan.Zero, Cancelled: false, Problems: []);
}
