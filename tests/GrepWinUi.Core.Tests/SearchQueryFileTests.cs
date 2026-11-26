using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.AppData.Session;

namespace GrepWinUi.Core.Tests;

/// <summary>Query files (File → Save / Open / Load search query) and the start folder of the file dialogs.</summary>
public sealed class SearchQueryFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public SearchQueryFileTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_KeepsAllValues()
    {
        string path = Path.Combine(_root, "query.json");
        SearchQuery query = new()
        {
            SearchString = @"[public|private]\s+function",
            RegularExpression = true,
            FileNames = "*.php",
            FolderName = @"e:\www",
            RegularExpressionOn = RegexTarget.PathName,
            ModifiedFrom = new DateTime(2026, 2, 3),
        };

        SearchQueryFile.Save(query, path);
        SearchQuery loaded = SearchQueryFile.Load(path);

        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(query), System.Text.Json.JsonSerializer.Serialize(loaded));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Save_WritesHeaderAndReadableJson()
    {
        string path = Path.Combine(_root, "query.json");

        SearchQueryFile.Save(new SearchQuery { SearchString = "x" }, path);
        string text = File.ReadAllText(path);

        Assert.Contains($"\"Kind\": \"{SearchQueryFile.Kind}\"", text);
        Assert.Contains("\"Version\": 1", text);
        Assert.Contains(Environment.NewLine, text);
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        string path = Path.Combine(_root, "query.json");
        SearchQueryFile.Save(new SearchQuery { SearchString = "first" }, path);

        SearchQueryFile.Save(new SearchQuery { SearchString = "second" }, path);

        Assert.Equal("second", SearchQueryFile.Load(path).SearchString);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{ \"ShowProgressInPercent\": true }")]
    [InlineData("{ \"Kind\": \"grepWinUi search query\" }")]
    [InlineData("null")]
    public void Load_NoQueryFile_ThrowsInvalidData(string content)
    {
        string path = Path.Combine(_root, "other.json");
        File.WriteAllText(path, content);

        Assert.Throws<InvalidDataException>(() => SearchQueryFile.Load(path));
    }

    [Fact]
    public void Load_MissingFile_ThrowsIOException()
    {
        Assert.ThrowsAny<IOException>(() => SearchQueryFile.Load(Path.Combine(_root, "missing.json")));
    }

    [Fact]
    public void StartFolder_FolderOfExistingLastFile()
    {
        Assert.Equal(_root, LastUsedFiles.StartFolder(Path.Combine(_root, "gone.json"), @"c:\fallback"));
    }

    [Fact]
    public void FileDialogFolder_AtTheBeginning_AppFolder()
    {
        Assert.Equal(@"c:\app", new LastUsedFiles().FileDialogFolder(@"c:\app"));
    }

    [Fact]
    public void FileDialogFolder_FolderOfLastUsedFileOfAnyKind()
    {
        LastUsedFiles lastUsed = new();
        lastUsed.RememberSearchQueryFile(@"z:\old\query.json");
        lastUsed.RememberResultFile(Path.Combine(_root, "result.json"));

        Assert.Equal(_root, lastUsed.FileDialogFolder(@"c:\app"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"z:\does\not\exist\query.json")]
    [InlineData("query.json")]
    public void StartFolder_NoUsableFolder_Fallback(string lastFile)
    {
        Assert.Equal(@"c:\fallback", LastUsedFiles.StartFolder(lastFile, @"c:\fallback"));
    }
}
