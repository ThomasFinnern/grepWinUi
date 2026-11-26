using GrepWinUi.Core.AppData.Session;
using SaveAndLoad;

namespace GrepWinUi.Core.Tests;

public sealed class LastUsedFilesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void RememberSearchQueryFile_SetsFieldAndLastUsedFile()
    {
        LastUsedFiles last = new();

        last.RememberSearchQueryFile(@"c:\q.json");

        Assert.Equal(@"c:\q.json", last.SearchQueryFile);
        Assert.Equal(@"c:\q.json", last.LastUsedFile);
    }

    [Fact]
    public void RememberResultFile_SetsFieldAndLastUsedFile()
    {
        LastUsedFiles last = new();

        last.RememberResultFile(@"c:\r.json");

        Assert.Equal(@"c:\r.json", last.ResultFile);
        Assert.Equal(@"c:\r.json", last.LastUsedFile);
    }

    [Fact]
    public void RememberResultHtmlFile_SetsFieldAndLastUsedFile()
    {
        LastUsedFiles last = new();

        last.RememberResultHtmlFile(@"c:\r.html");

        Assert.Equal(@"c:\r.html", last.ResultHtmlFile);
        Assert.Equal(@"c:\r.html", last.LastUsedFile);
    }

    [Fact]
    public void OrLastUsedFile_EmptyFile_FallsBackToNewestFile()
    {
        LastUsedFiles last = new();
        last.RememberSearchQueryFile(@"c:\q.json");

        Assert.Equal(@"c:\q.json", last.OrLastUsedFile(last.ResultFile));
    }

    [Fact]
    public void OrLastUsedFile_SetFile_ReturnsIt()
    {
        LastUsedFiles last = new();
        last.RememberResultFile(@"c:\r.json");
        last.RememberSearchQueryFile(@"c:\q.json");

        Assert.Equal(@"c:\r.json", last.OrLastUsedFile(last.ResultFile));
    }

    [Fact]
    public void SaveThenLoad_KeepsAllFiles()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "NoDefaults")));
        LastUsedFiles saved = new();
        saved.RememberResultHtmlFile(@"c:\r.html");
        saved.RememberResultFile(@"c:\r.json");
        saved.RememberSearchQueryFile(@"c:\q.json");

        store.Save(saved);
        LastUsedFiles loaded = store.Load<LastUsedFiles>().Value;

        Assert.Equal(@"c:\q.json", loaded.SearchQueryFile);
        Assert.Equal(@"c:\r.json", loaded.ResultFile);
        Assert.Equal(@"c:\r.html", loaded.ResultHtmlFile);
        Assert.Equal(@"c:\q.json", loaded.LastUsedFile);
    }
}
