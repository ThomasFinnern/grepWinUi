using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>"Only in files of current result": search only the files of the previous result (temp folders only).</summary>
public sealed class SearchInFilesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public SearchInFilesTests()
    {
        Write("a.cs", "alpha\nbeta");
        Write("b.cs", "beta only");
        Write(@"sub\c.txt", "alpha in text");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Run_OnlyFiles_SearchesThoseFilesInOrderWithoutFolderWalk()
    {
        // File names "*.cs" and the folder are not used: c.txt is searched although it doesn't match "*.cs".
        SearchQuery query = new() { SearchString = "alpha", FileNames = "*.cs", FolderName = @"c:\no\such\folder" };

        SearchResult result = SearchEngine.Run(query, null, CancellationToken.None,
            onlyFiles: [At(@"sub\c.txt"), At("b.cs"), At("a.cs")]);

        Assert.Equal([At(@"sub\c.txt"), At("a.cs")], result.Files.Select(file => file.Path));
        Assert.Equal(3, result.FilesSearched);
        Assert.Equal(0, result.FoldersSearched);
        Assert.Empty(result.Problems);
        Assert.True(result.InFilesOfPreviousResult);
    }

    [Fact]
    public void Run_OnlyFiles_MissingFileIsProblemDuplicateOnce()
    {
        SearchQuery query = new() { SearchString = "beta" };

        SearchResult result = SearchEngine.Run(query, null, CancellationToken.None,
            onlyFiles: [At("a.cs"), At("gone.cs"), At("A.CS")]);

        Assert.Equal(1, result.FilesSearched);
        Assert.Single(result.Files);
        Assert.Equal([$"File not found: '{At("gone.cs")}'"], result.Problems);
    }

    [Fact]
    public void Run_OnlyFiles_CountFilesFirstKnowsTotal()
    {
        SearchQuery query = new() { SearchString = "beta", CountFilesFirst = true };
        List<SearchProgress> reports = [];

        SearchEngine.Run(query, new SyncProgress(reports.Add), CancellationToken.None, onlyFiles: [At("a.cs"), At("b.cs")]);

        Assert.Equal(2, reports[^1].TotalFiles);
    }

    [Fact]
    public void Run_WithoutOnlyFiles_NotMarked()
    {
        SearchQuery query = new() { SearchString = "alpha", FolderName = _root, FileNames = "*.*" };

        SearchResult result = SearchEngine.Run(query, null, CancellationToken.None);

        Assert.False(result.InFilesOfPreviousResult);
        Assert.Equal(2, result.Files.Count);
    }

    [Fact]
    public void Scope_OptionOff_Folders()
    {
        Assert.Equal(new SearchScope(null, null), SearchScope.Of(new SearchQuery(), Result(At("a.cs"))));
    }

    [Fact]
    public void Scope_OptionOn_FilesOfCurrentResult()
    {
        SearchScope scope = SearchScope.Of(new SearchQuery { OnlyInFilesOfCurrentResult = true }, Result(At("a.cs"), At("b.cs")));

        Assert.Equal([At("a.cs"), At("b.cs")], scope.OnlyFiles);
        Assert.Null(scope.Problem);
    }

    [Fact]
    public void Scope_OptionOnWithoutFiles_FoldersWithNote()
    {
        SearchQuery query = new() { OnlyInFilesOfCurrentResult = true };

        Assert.Equal(new SearchScope(null, SearchScope.NoCurrentFilesProblem), SearchScope.Of(query, null));
        Assert.Equal(new SearchScope(null, SearchScope.NoCurrentFilesProblem), SearchScope.Of(query, Result()));
    }

    [Fact]
    public void Summary_InFilesOfPreviousResult()
    {
        SearchResult result = Result(At("a.cs")) with { InFilesOfPreviousResult = true };

        Assert.StartsWith("In the files of the previous result: found ", ResultPageHtml.Summary(result));
        Assert.DoesNotContain("folder", ResultPageHtml.Summary(result));
    }

    [Fact]
    public void ResultFile_KeepsFlag_OldFileWithoutFlagIsFalse()
    {
        string path = At("r.result.json");
        SearchResultFile.Save(Result(At("a.cs")) with { InFilesOfPreviousResult = true }, path);
        Assert.True(SearchResultFile.Load(path).InFilesOfPreviousResult);

        File.WriteAllText(path, File.ReadAllText(path).Replace("\"InFilesOfPreviousResult\": true,", "").Replace(",\n    \"InFilesOfPreviousResult\": true", "").Replace(",\r\n    \"InFilesOfPreviousResult\": true", ""));
        Assert.DoesNotContain("InFilesOfPreviousResult", File.ReadAllText(path));
        Assert.False(SearchResultFile.Load(path).InFilesOfPreviousResult);
    }

    private static SearchResult Result(params string[] paths) => new(
        new SearchQuery { SearchString = "x" },
        [.. paths.Select(path => new FileResult(path, [new LineMatch(1, "x", [new Hit(0, 1)])]))],
        paths.Length, 1, paths.Length, TimeSpan.Zero, false, []);

    private string At(string relative) => Path.Combine(_root, relative);

    private void Write(string relative, string text)
    {
        string path = At(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    /// <summary>Reports on the calling thread.</summary>
    private sealed class SyncProgress(Action<SearchProgress> report) : IProgress<SearchProgress>
    {
        public void Report(SearchProgress value) => report(value);
    }
}
