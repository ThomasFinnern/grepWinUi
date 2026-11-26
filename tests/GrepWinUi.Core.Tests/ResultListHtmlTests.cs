using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Plain result lists for the two WebView2 panes.</summary>
public sealed class ResultListHtmlTests
{
    [Fact]
    public void MarkHits_WrapsEveryHitAndEncodesText()
    {
        LineMatch line = new(3, "a<b> FOO x foo", [new Hit(5, 3), new Hit(11, 3)]);

        Assert.Equal("a&lt;b&gt; <mark>FOO</mark> x <mark>foo</mark>", ResultListHtml.MarkHits(line));
    }

    [Fact]
    public void FileList_SummaryProblemsAndFilesWithHitCount()
    {
        SearchResult result = Result(cancelled: false, problems: ["File not readable: 'x'"]);

        string html = ResultListHtml.FileList(result, "#F0F0F0");

        Assert.Contains("&#39;foo&#39; in &#39;*.cs&#39;: 3 hits in 2 of 7 files", html);
        Assert.Contains("File not readable: &#39;x&#39;", html);
        Assert.Contains(@"<li>c:\src\a.cs <span class=""count"">(2)</span></li>", html);
        Assert.Contains(@"<li>c:\src\b&amp;c.cs <span class=""count"">(1)</span></li>", html);
        Assert.Contains("background: #F0F0F0", html);
    }

    [Fact]
    public void Summary_Cancelled_Marked()
    {
        Assert.EndsWith("— cancelled", ResultListHtml.Summary(Result(cancelled: true, problems: [])));
    }

    [Fact]
    public void MatchingLines_FileHeadersAndNumberedLines()
    {
        string html = ResultListHtml.MatchingLines(Result(cancelled: false, problems: []), "white");

        Assert.Contains(@"<p class=""file"">c:\src\a.cs</p>", html);
        Assert.Contains(@"<span class=""num"">12:</span> x <mark>foo</mark> <mark>foo</mark>", html);
        Assert.True(html.IndexOf(@"a.cs", StringComparison.Ordinal) < html.IndexOf(@"b&amp;c.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void MatchingLines_TooManyLines_CutWithNote()
    {
        LineMatch[] lines = [.. Enumerable.Range(1, ResultListHtml.MaxLines + 10).Select(i => new LineMatch(i, "foo", [new Hit(0, 3)]))];
        SearchResult result = new(new SearchQuery(), [new FileResult("big.txt", lines)], 1, 1, lines.Length, TimeSpan.Zero, false, []);

        string html = ResultListHtml.MatchingLines(result, "white");

        Assert.Contains($"only the first {ResultListHtml.MaxLines} lines", html);
        Assert.DoesNotContain($">{ResultListHtml.MaxLines + 1}:<", html);
    }

    private static SearchResult Result(bool cancelled, IReadOnlyList<string> problems) => new(
        new SearchQuery { SearchString = "foo", FileNames = "*.cs", FolderName = @"c:\src" },
        [
            new FileResult(@"c:\src\a.cs", [new LineMatch(12, "x foo foo", [new Hit(2, 3), new Hit(6, 3)])]),
            new FileResult(@"c:\src\b&c.cs", [new LineMatch(1, "foo", [new Hit(0, 3)])]),
        ],
        FilesSearched: 7,
        FoldersSearched: 2,
        MatchCount: 3,
        Duration: TimeSpan.FromSeconds(0.4),
        Cancelled: cancelled,
        Problems: problems);
}
