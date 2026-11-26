using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Result modes and Match whole line (temp folders only).</summary>
public sealed class ResultModeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public ResultModeTests()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "a.txt"), "foo 1\nbar\nfoo 3\nbaz\nfoo 5");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "nothing\nhere");
        File.WriteAllText(Path.Combine(_root, "c.txt"), "  foo  \nfoo bar\nFOO");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Normal_AllMatchingLines()
    {
        SearchResult result = Run(Query());

        Assert.Equal(["a.txt", "c.txt"], Names(result));
        Assert.Equal([1, 3, 5], LineNumbers(result, "a.txt"));
        Assert.Equal(6, result.MatchCount);
    }

    [Fact]
    public void FirstMatchOnly_FirstLineThenStops()
    {
        SearchQuery query = Query();
        query.FirstMatchPerFileOnly = true;

        SearchResult result = Run(query);

        Assert.Equal([1], LineNumbers(result, "a.txt"));
        Assert.Equal([1], LineNumbers(result, "c.txt"));
        Assert.Equal(2, result.MatchCount);
    }

    [Fact]
    public void FirstMatchOnly_ReadsOnForLinesAfter()
    {
        SearchQuery query = Query();
        query.FirstMatchPerFileOnly = true;

        SearchResult result = SearchEngine.Run(query, null, CancellationToken.None, new KeepLines(0, 2));
        FileResult a = result.Files.Single(file => Path.GetFileName(file.Path) == "a.txt");

        Assert.Equal([1], a.Lines.Select(line => line.LineNumber));
        Assert.Equal([2, 3], a.ContextLines.Select(line => line.LineNumber));
    }

    [Fact]
    public void FilesWithoutMatch_OnlyThoseWithoutLines()
    {
        SearchQuery query = Query();
        query.ListFilesWithoutMatch = true;

        SearchResult result = Run(query);

        Assert.Equal(["b.txt"], Names(result));
        Assert.Empty(result.Files[0].Lines);
        Assert.Equal(0, result.MatchCount);
        Assert.Equal("1 files without match from 3 files searched", ResultMode.Counts(result));
    }

    [Fact]
    public void FileNamesOnly_FilesWithMatchNoLines()
    {
        SearchQuery query = Query();
        query.FileNamesOnly = true;

        SearchResult result = Run(query);

        Assert.Equal(["a.txt", "c.txt"], Names(result));
        Assert.All(result.Files, file => Assert.Empty(file.Lines));
        Assert.Equal("2 files with matches from 3 files searched", ResultMode.Counts(result));
    }

    [Fact]
    public void NonMatchingLines_LinesWithoutHitsCounted()
    {
        SearchQuery query = Query();
        query.NonMatchingLines = true;

        SearchResult result = Run(query);

        Assert.Equal(["a.txt", "b.txt"], Names(result));
        Assert.Equal([2, 4], LineNumbers(result, "a.txt"));
        Assert.All(result.Files.SelectMany(file => file.Lines), line => Assert.Empty(line.Hits));
        Assert.Equal(4, result.MatchCount);
        Assert.Equal("4 lines without match in 2 files from 3 files searched", ResultMode.Counts(result));
    }

    [Fact]
    public void NonMatchingLinesAndFilesWithoutMatch_FilesWhereEveryLineMatches()
    {
        SearchQuery query = Query();
        query.NonMatchingLines = true;
        query.ListFilesWithoutMatch = true;

        Assert.Equal(["c.txt"], Names(Run(query)));
    }

    [Fact]
    public void MatchWholeLine_PlainAndRegex_BlanksAroundIgnored()
    {
        SearchQuery query = Query();
        query.MatchWholeLine = true;

        SearchResult plain = Run(query);
        Assert.Equal(["c.txt"], Names(plain));
        Assert.Equal([1, 3], LineNumbers(plain, "c.txt"));
        Assert.Equal([new Hit(2, 3)], plain.Files[0].Lines[0].Hits);

        query.SearchString = "fo+";
        query.RegularExpression = true;
        SearchResult regex = Run(query);
        Assert.Equal([1, 3], LineNumbers(regex, "c.txt"));
        Assert.Equal([new Hit(2, 3)], regex.Files[0].Lines[0].Hits);
    }

    [Fact]
    public void FileList_CountColumnPerMode()
    {
        SearchQuery query = Query();
        Assert.Contains("<th class=\"Matches\">Matches</th>", ResultPageHtml.FileList(Run(query), "", "white"));

        query.NonMatchingLines = true;
        string lines = ResultPageHtml.FileList(Run(query), "", "white");
        Assert.Contains("<th class=\"Matches\">Lines</th>", lines);
        Assert.Contains("<td class=\"Matches\">2</td>", lines);

        query.NonMatchingLines = false;
        query.FileNamesOnly = true;
        Assert.DoesNotContain("class=\"Matches\"", ResultPageHtml.FileList(Run(query), "", "white"));
    }

    [Fact]
    public void MatchingLines_HeaderAndNotFoundPerMode()
    {
        SearchQuery query = Query();
        query.ListFilesWithoutMatch = true;
        Assert.Equal("Files without", ResultPageHtml.LinesTitleKind(query));

        query.SearchString = "nothing|here|foo|bar|baz";
        query.RegularExpression = true;
        string html = ResultPageHtml.MatchingLines(Run(query), new ResultViewOptions(), "", "white");
        Assert.Contains("<h1>Files without '", html);
        Assert.Contains("Found no files without match for", html);

        query.ListFilesWithoutMatch = false;
        query.NonMatchingLines = true;
        Assert.Equal("Lines without", ResultPageHtml.LinesTitleKind(query));
        query.NonMatchingLines = false;
        query.FileNamesOnly = true;
        Assert.Equal("Files with", ResultPageHtml.LinesTitleKind(query));
    }

    [Fact]
    public void SearchStatusDone_UsesModeCounts()
    {
        SearchQuery query = Query();
        query.FileNamesOnly = true;

        Assert.Equal("2 files with matches from 3 files searched", SearchStatus.Done(Run(query)).Info);
    }

    private SearchQuery Query() => new() { SearchString = "foo", FileNames = "*.txt", FolderName = $"\"{_root}\"" };

    private static SearchResult Run(SearchQuery query) => SearchEngine.Run(query, null, CancellationToken.None);

    private static List<string> Names(SearchResult result) => [.. result.Files.Select(file => Path.GetFileName(file.Path))];

    private static List<int> LineNumbers(SearchResult result, string name) =>
        [.. result.Files.Single(file => Path.GetFileName(file.Path) == name).Lines.Select(line => line.LineNumber)];
}
