using System.Text;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Basic search engine: files, lines, hits, cancel, progress (temp folders only).</summary>
public sealed class SearchEngineTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public SearchEngineTests()
    {
        Write("a.cs", "class A\npublic function one()\nnothing here");
        Write("b.md", "# Function list\nfunction FUNCTION function");
        Write("c.txt", "function in text");
        Write(@"sub\d.cs", "private function two()");
        Write(@"sub\deeper\e.CS", "no match");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void FileFinder_PatternsAndSubfolders_EachFileOnceFolderBeforeSubfolders()
    {
        FileFinder finder = new();

        List<string> files = [.. finder.Find(_root, "*.cs *.CS a.*", includeSubfolders: true, CancellationToken.None)];

        Assert.Equal(["a.cs", @"sub\d.cs", @"sub\deeper\e.CS"], files.Select(Relative));
        Assert.Equal(3, finder.FoldersSearched);
        Assert.Empty(finder.Problems);
    }

    [Fact]
    public void FileFinder_WithoutSubfolders_OnlyTopFolder()
    {
        List<string> files = [.. new FileFinder().Find(_root, "*.cs", includeSubfolders: false, CancellationToken.None)];

        Assert.Equal(["a.cs"], files.Select(Relative));
    }

    [Theory]
    [InlineData("*.*")]
    [InlineData("")]
    [InlineData("  ")]
    public void FileFinder_AllFilesPatterns(string fileNames)
    {
        Assert.Equal(5, new FileFinder().Find(_root, fileNames, includeSubfolders: true, CancellationToken.None).Count());
    }

    [Fact]
    public void FileFinder_QuotedFolder_Works()
    {
        Assert.Single(new FileFinder().Find($" \"{_root}\" ", "*.md", includeSubfolders: true, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"z:\does\not\exist")]
    public void FileFinder_NoOrMissingFolder_ProblemNoFiles(string folder)
    {
        FileFinder finder = new();

        Assert.Empty(finder.Find(folder, "*.*", includeSubfolders: true, CancellationToken.None));
        Assert.Single(finder.Problems);
    }

    [Fact]
    public void LineMatcher_AllHitsIgnoringCase_NotOverlapping()
    {
        LineMatcher matcher = LineMatcher.PlainText("aa");

        Assert.Equal([new Hit(0, 2), new Hit(2, 2), new Hit(6, 2)], matcher.Find("aAaa  AA"));
        Assert.Empty(matcher.Find("a"));
    }

    [Fact]
    public void Run_FindsLinesWithNumbersAndHits()
    {
        SearchResult result = SearchEngine.Run(Query("function", "*.cs *.md"), null, CancellationToken.None);

        Assert.Equal(["a.cs", "b.md", @"sub\d.cs"], result.Files.Select(file => Relative(file.Path)));
        LineMatch line = Assert.Single(result.Files[0].Lines);
        Assert.Equal(2, line.LineNumber);
        Assert.Equal("public function one()", line.Text);
        Assert.Equal([new Hit(7, 8)], line.Hits);
        Assert.Equal([1, 2], result.Files[1].Lines.Select(l => l.LineNumber));
        Assert.Equal(3, result.Files[1].Lines[1].Hits.Count);
        Assert.Equal(1 + 4 + 1, result.MatchCount);
        Assert.Equal(4, result.FilesSearched);
        Assert.False(result.Cancelled);
        Assert.Empty(result.Problems);
    }

    [Fact]
    public void Run_EmptySearchString_NoSearchProblem()
    {
        SearchResult result = SearchEngine.Run(Query("", "*.*"), null, CancellationToken.None);

        Assert.Empty(result.Files);
        Assert.Equal(0, result.FilesSearched);
        Assert.Single(result.Problems);
    }

    [Fact]
    public void Run_Utf16FileWithByteOrderMark_IsRead()
    {
        File.WriteAllText(Path.Combine(_root, "wide.txt"), "Grüße function", Encoding.Unicode);

        SearchResult result = SearchEngine.Run(Query("grüße", "wide.txt"), null, CancellationToken.None);

        Assert.Equal("Grüße function", Assert.Single(Assert.Single(result.Files).Lines).Text);
    }

    [Fact]
    public void Run_Cancelled_ReturnsPartialResultWithoutException()
    {
        using CancellationTokenSource cancel = new();
        cancel.Cancel();

        SearchResult result = SearchEngine.Run(Query("function", "*.*"), null, cancel.Token);

        Assert.True(result.Cancelled);
        Assert.Empty(result.Files);
    }

    [Fact]
    public void Run_ReportsFinalProgress()
    {
        ListProgress progress = new();

        SearchEngine.Run(Query("function", "*.*"), progress, CancellationToken.None);

        SearchProgress last = progress.Reports[^1];
        Assert.Equal(5, last.FilesSearched);
        Assert.Equal(3, last.FoldersSearched);
        Assert.Equal(4, last.FilesMatched);
    }

    [Fact]
    public async Task RunAsync_SameResultAsRun()
    {
        SearchResult result = await SearchEngine.RunAsync(Query("function", "*.*"), null, CancellationToken.None);

        Assert.Equal(4, result.Files.Count);
    }

    private SearchQuery Query(string text, string fileNames) => new() { SearchString = text, FileNames = fileNames, FolderName = _root };

    private void Write(string relative, string text)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private string Relative(string path) => Path.GetRelativePath(_root, path);

    /// <summary>Collects progress reports on the calling thread.</summary>
    private sealed class ListProgress : IProgress<SearchProgress>
    {
        public List<SearchProgress> Reports { get; } = [];

        public void Report(SearchProgress value) => Reports.Add(value);
    }
}
