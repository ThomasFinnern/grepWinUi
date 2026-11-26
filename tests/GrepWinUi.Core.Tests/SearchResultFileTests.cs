using System.Text.Json;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Result files (File → Save / Open search result).</summary>
public sealed class SearchResultFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public SearchResultFileTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_KeepsParametersFilesLinesAndHits()
    {
        string path = Path.Combine(_root, "r" + SearchResultFile.Extension);
        SearchResult saved = Sample();

        SearchResultFile.Save(saved, path);
        SearchResult loaded = SearchResultFile.Load(path);

        Assert.Equal(JsonSerializer.Serialize(saved.Query), JsonSerializer.Serialize(loaded.Query));
        Assert.Equal(2, loaded.Files.Count);
        LineMatch line = loaded.Files[0].Lines[0];
        Assert.Equal(12, line.LineNumber);
        Assert.Equal("x foo foo", line.Text);
        Assert.Equal([new Hit(2, 3), new Hit(6, 3)], line.Hits);
        Assert.Equal(@"c:\src\b.cs", loaded.Files[1].Path);
        Assert.Equal((7, 2, 3), (loaded.FilesSearched, loaded.FoldersSearched, loaded.MatchCount));
        Assert.Equal(TimeSpan.FromSeconds(0.4), loaded.Duration);
        Assert.True(loaded.Cancelled);
        Assert.Equal(["File not readable: 'x'"], loaded.Problems);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Save_WritesHeader()
    {
        string path = Path.Combine(_root, "r" + SearchResultFile.Extension);

        SearchResultFile.Save(Sample(), path);
        string text = File.ReadAllText(path);

        Assert.Contains($"\"Kind\": \"{SearchResultFile.Kind}\"", text);
        Assert.Contains("\"Version\": 1", text);
        Assert.Contains("\"Saved\":", text);
    }

    [Fact]
    public void Load_QueryFileOrOtherJson_ThrowsInvalidData()
    {
        string queryFile = Path.Combine(_root, "q.json");
        SearchQueryFile.Save(new SearchQuery { SearchString = "x" }, queryFile);
        string other = Path.Combine(_root, "o.json");
        File.WriteAllText(other, "{ \"Kind\": \"grepWinUi search result\" }");
        string broken = Path.Combine(_root, "b.json");
        File.WriteAllText(broken, "{ not json");

        Assert.Throws<InvalidDataException>(() => SearchResultFile.Load(queryFile));
        Assert.Throws<InvalidDataException>(() => SearchResultFile.Load(other));
        Assert.Throws<InvalidDataException>(() => SearchResultFile.Load(broken));
    }

    [Fact]
    public void QueryFileLoad_RejectsResultFile()
    {
        string path = Path.Combine(_root, "r" + SearchResultFile.Extension);
        SearchResultFile.Save(Sample(), path);

        Assert.Throws<InvalidDataException>(() => SearchQueryFile.Load(path));
    }

    [Theory]
    [InlineData(@"c:\a\result", @"c:\a\result.result.json")]
    [InlineData(@"c:\a\result.json", @"c:\a\result.result.json")]
    [InlineData(@"c:\a\result.RESULT.json", @"c:\a\result.RESULT.json")]
    [InlineData(@"c:\a\result.txt", @"c:\a\result.txt.result.json")]
    public void WithExtension_AddsOrReplaces(string path, string expected)
    {
        Assert.Equal(expected, SearchResultFile.WithExtension(path));
    }

    private static SearchResult Sample() => new(
        new SearchQuery { SearchString = "foo", FileNames = "*.cs", FolderName = @"c:\src", IncludeSubfolders = false },
        [
            new FileResult(@"c:\src\a.cs", [new LineMatch(12, "x foo foo", [new Hit(2, 3), new Hit(6, 3)])]),
            new FileResult(@"c:\src\b.cs", [new LineMatch(1, "foo", [new Hit(0, 3)])]),
        ],
        FilesSearched: 7,
        FoldersSearched: 2,
        MatchCount: 3,
        Duration: TimeSpan.FromSeconds(0.4),
        Cancelled: true,
        Problems: ["File not readable: 'x'"]);
}
