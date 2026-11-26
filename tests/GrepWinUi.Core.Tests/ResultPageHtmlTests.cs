using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>HTML pages of the two result panes (file list, matching lines) with the pane CSS.</summary>
public sealed class ResultPageHtmlTests
{
    private const string Css = ".Token { color: green; }";
    private static readonly ResultViewOptions View = new();

    /// <summary>Matches at lines 5, 7 and 20; kept lines 3, 4, 6, 8, 9, 18, 19, 21.</summary>
    private static readonly FileResult ContextFile = new(
        @"c:\src\a.cs",
        [Match(5), Match(7), Match(20)],
        [.. new[] { 3, 4, 6, 8, 9, 18, 19, 21 }.Select(number => new ContextLine(number, $"line {number}"))]);

    private static LineMatch Match(int number) => new(number, "foo", [new Hit(0, 3)]);

    [Fact]
    public void ShownLines_NoLinesAround_OnlyMatchesNoGaps()
    {
        List<ResultPageHtml.ShownLine> shown = [.. ResultPageHtml.ShownLines(ContextFile, 0, 0)];

        Assert.Equal([5, 7, 20], shown.Select(line => line.LineNumber));
        Assert.DoesNotContain(shown, line => line.GapBefore);
    }

    [Fact]
    public void ShownLines_BeforeAndAfter_EachLineOnceStopAtNextMatch()
    {
        List<ResultPageHtml.ShownLine> shown = [.. ResultPageHtml.ShownLines(ContextFile, 2, 2)];

        Assert.Equal([3, 4, 5, 6, 7, 8, 9, 18, 19, 20, 21], shown.Select(line => line.LineNumber));
        Assert.Equal([5, 7, 20], shown.Where(line => line.Match is not null).Select(line => line.LineNumber));
        Assert.Equal([18], shown.Where(line => line.GapBefore).Select(line => line.LineNumber));
    }

    [Fact]
    public void ShownLines_FewerShownThanKept()
    {
        List<ResultPageHtml.ShownLine> shown = [.. ResultPageHtml.ShownLines(ContextFile, 1, 0)];

        Assert.Equal([4, 5, 6, 7, 19, 20], shown.Select(line => line.LineNumber));
        Assert.Equal([19], shown.Where(line => line.GapBefore).Select(line => line.LineNumber));
    }

    [Fact]
    public void ShownLines_MoreShownThanKept_ShowsWhatWasKept()
    {
        FileResult file = new(@"c:\a.cs", [Match(10)], [new ContextLine(9, "nine")]);

        Assert.Equal([9, 10], ResultPageHtml.ShownLines(file, 5, 5).Select(line => line.LineNumber));
    }

    [Fact]
    public void MatchingLines_ContextLinesAndGap()
    {
        SearchResult result = new(new SearchQuery { SearchString = "foo" }, [ContextFile], 1, 1, 3, TimeSpan.Zero, false, []);

        string html = ResultPageHtml.MatchingLines(result, new ResultViewOptions { ShowLinesBefore = 2, ShowLinesAfter = 2 }, Css, "white");

        Assert.Contains(@"<div class=""Line ContextLine""><span class=""LineNumber"">000004: </span>line 4</div>", html);
        Assert.Contains(@"<div class=""Line""><span class=""LineNumber"">000005: </span><span class=""Token"">foo</span></div>", html);
        Assert.Contains(@"line 9</div><div class=""Gap""></div><div class=""Line ContextLine""><span class=""LineNumber"">000018: ", html);
    }

    [Fact]
    public void MatchingLines_WithoutLinesAround_NoContextNoGap()
    {
        SearchResult result = new(new SearchQuery { SearchString = "foo" }, [ContextFile], 1, 1, 3, TimeSpan.Zero, false, []);

        string html = ResultPageHtml.MatchingLines(result, View, Css, "white");

        Assert.DoesNotContain("ContextLine", html);
        Assert.DoesNotContain("Gap", html);
    }

    [Fact]
    public void MarkHits_WrapsEveryHitInTokenSpanAndEncodesText()
    {
        LineMatch line = new(3, "a<b> FOO x foo", [new Hit(5, 3), new Hit(11, 3)]);

        Assert.Equal(@"a&lt;b&gt; <span class=""Token"">FOO</span> x <span class=""Token"">foo</span>", ResultPageHtml.MarkHits(line));
    }

    [Fact]
    public void Page_HasBackgroundRuleThenCss()
    {
        string html = ResultPageHtml.FileList(Result(cancelled: false, problems: []), Css, "#F0F0F0");

        int background = html.IndexOf("body { background: #F0F0F0; }", StringComparison.Ordinal);
        int css = html.IndexOf(Css, StringComparison.Ordinal);
        Assert.True(background >= 0 && css > background, "CSS must follow the background rule so it can override it");
        Assert.True(css < html.IndexOf("</style>", StringComparison.Ordinal));
    }

    [Fact]
    public void FileList_HeaderProblemsAndTable()
    {
        string html = ResultPageHtml.FileList(Result(cancelled: false, problems: ["File not readable: 'x'"]), Css, "white");

        Assert.Contains(@"<div class=""HeaderFoundFiles""><h1>Results for searched string: 'foo'</h1>", html);
        Assert.Contains("found 3 matches in 2 files from 7 files searched in folder &#39;c:\\src&#39;", html);
        Assert.Contains(@"<p class=""Problem"">File not readable: &#39;x&#39;</p>", html);
        Assert.Contains(@"<td class=""FileName""><div title=""a.cs"">a.cs</div></td><td class=""Folder"">c:\src</td><td class=""Matches"">2</td>", html);
        Assert.Contains(@"<td class=""FileName""><div title=""b&amp;c.cs"">b&amp;c.cs</div></td>", html);
        Assert.Contains(@"<th class=""FileName"">File name</th><th class=""Folder"">Folder</th><th class=""Matches"">Matches</th>", html);
    }

    [Fact]
    public void FileList_NoFiles_NotFoundInsteadOfTable()
    {
        SearchResult result = Result(cancelled: false, problems: []) with { Files = [], MatchCount = 0 };

        string html = ResultPageHtml.FileList(result, Css, "white");

        Assert.Contains(@"<p class=""NotFound"">Found no matches for 'foo'</p>", html);
        Assert.DoesNotContain("<table>", html);
    }

    [Fact]
    public void Summary_Cancelled_Marked()
    {
        Assert.EndsWith("— cancelled", ResultPageHtml.Summary(Result(cancelled: true, problems: [])));
        Assert.DoesNotContain("cancelled", ResultPageHtml.Summary(Result(cancelled: false, problems: [])));
    }

    [Fact]
    public void NotFound_Cancelled_Marked()
    {
        SearchResult result = Result(cancelled: true, problems: []) with { Files = [] };

        Assert.Contains("Found no matches for 'foo' (search cancelled)", ResultPageHtml.MatchingLines(result, View, Css, "white"));
    }

    [Fact]
    public void MatchingLines_HeaderFileTitlesAndNumberedLines()
    {
        string html = ResultPageHtml.MatchingLines(Result(cancelled: false, problems: []), View, Css, "white");

        Assert.Contains(@"<div class=""HeaderFoundToken""><h1>Lines with 'foo'</h1></div>", html);
        Assert.Contains(@"<div class=""FileNameTitle"">c:\src\a.cs</div>", html);
        Assert.Contains(@"<div class=""Line""><span class=""LineNumber"">000012: </span>x <span class=""Token"">foo</span>", html);
        Assert.True(html.IndexOf("a.cs", StringComparison.Ordinal) < html.IndexOf("b&amp;c.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void LineNumber_SixDigits()
    {
        Assert.Equal("000007", ResultPageHtml.LineNumber(7));
        Assert.Equal("1234567", ResultPageHtml.LineNumber(1234567));
    }

    [Fact]
    public void MatchingLines_TooManyLines_CutWithNote()
    {
        LineMatch[] lines = [.. Enumerable.Range(1, ResultPageHtml.MaxLines + 10).Select(i => new LineMatch(i, "foo", [new Hit(0, 3)]))];
        SearchResult result = new(new SearchQuery(), [new FileResult("big.txt", lines)], 1, 1, lines.Length, TimeSpan.Zero, false, []);

        string html = ResultPageHtml.MatchingLines(result, View, Css, "white");

        Assert.Contains($"only the first {ResultPageHtml.MaxLines} lines", html);
        Assert.DoesNotContain($">{ResultPageHtml.LineNumber(ResultPageHtml.MaxLines + 1)}: <", html);
        Assert.Contains($">{ResultPageHtml.LineNumber(ResultPageHtml.MaxLines)}: <", html);
    }

    [Fact]
    public void Hint_EncodedText_EmptyTextNoParagraph()
    {
        Assert.Contains("<p>a &lt; b</p>", ResultPageHtml.Hint("a < b", Css, "white"));
        Assert.Contains("<body></body>", ResultPageHtml.Hint("", Css, "white"));
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
