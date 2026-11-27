using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.AppData.Session;
using GrepWinUi.Core.Search;
using SaveAndLoad;

namespace GrepWinUi.Core.Tests;

/// <summary>Result windows open on exit, reopened on the next start.</summary>
public sealed class ReopenSearchesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    private string Folder => Path.Combine(_root, ReopenSearches.FolderName);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void WriteWindows_ResultFileOnlyForWindowsWithResult()
    {
        ReopenSearches list = ReopenSearches.WriteWindows(Folder,
            [Window("a", withResult: true), Window("b", withResult: false), Window("c", withResult: true)], activeIndex: 1);

        Assert.Equal(["a", "b", "c"], list.Entries.Select(entry => entry.Query.SearchString));
        Assert.Equal(Path.Combine(Folder, "Search_0.result.json"), list.Entries[0].ResultFile);
        Assert.Equal("", list.Entries[1].ResultFile);
        Assert.Equal(Path.Combine(Folder, "Search_2.result.json"), list.Entries[2].ResultFile);
        Assert.Equal("c", SearchResultFile.Load(list.Entries[2].ResultFile).Query.SearchString);
        Assert.Equal(1, list.ActiveIndex);
        Assert.Equal("b", list.Active?.Query.SearchString);
    }

    [Fact]
    public void WriteWindows_KeepsWindowQueryAndViewOptions()
    {
        ReopenWindow window = Window("old", withResult: true) with
        {
            Query = new SearchQuery { SearchString = "edited later" },
            View = new ResultViewOptions { KeepLinesBefore = 3, ShowLinesBefore = 2, ShowTitle = false },
        };

        ReopenEntry entry = ReopenSearches.WriteWindows(Folder, [window], 0).Entries[0];

        Assert.Equal("edited later", entry.Query.SearchString);
        Assert.Equal("old", SearchResultFile.Load(entry.ResultFile).Query.SearchString);
        Assert.Equal(window.View, entry.View);
        Assert.NotSame(window.View, entry.View);
        Assert.NotSame(window.Query, entry.Query);
    }

    [Fact]
    public void WriteWindows_DeletesOldWindowFilesOnly()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, "Search_7.result.json"), "old");
        File.WriteAllText(Path.Combine(Folder, "notes.txt"), "keep");

        ReopenSearches.WriteWindows(Folder, [Window("a", withResult: true)], 0);

        Assert.False(File.Exists(Path.Combine(Folder, "Search_7.result.json")));
        Assert.True(File.Exists(Path.Combine(Folder, "notes.txt")));
        Assert.True(File.Exists(Path.Combine(Folder, "Search_0.result.json")));
    }

    [Fact]
    public void WriteWindows_NoWindows_EmptyListNoActive()
    {
        ReopenSearches list = ReopenSearches.WriteWindows(Folder, [], activeIndex: 0);

        Assert.Empty(list.Entries);
        Assert.Equal(-1, list.ActiveIndex);
        Assert.Null(list.Active);
    }

    [Fact]
    public void ToOpen_AllActiveOnlyOrNone()
    {
        ReopenSearches list = ReopenSearches.WriteWindows(Folder, [Window("a", true), Window("b", true)], activeIndex: 1);

        Assert.Equal(2, list.ToOpen(reopenAll: true, activeOnly: true).Count);
        Assert.Equal(["b"], list.ToOpen(reopenAll: false, activeOnly: true).Select(entry => entry.Query.SearchString));
        Assert.Empty(list.ToOpen(reopenAll: false, activeOnly: false));
        Assert.Empty(new ReopenSearches().ToOpen(reopenAll: false, activeOnly: true));
    }

    [Fact]
    public void List_SaveAndLoad()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "user"), Path.Combine(_root, "defaults")));
        ReopenSearches list = ReopenSearches.WriteWindows(Folder,
            [Window("a", true), Window("b", false) with { View = new ResultViewOptions { FixedFont = false } }], 0);

        store.Save(list);
        ReopenSearches loaded = store.Load<ReopenSearches>().Value;

        Assert.Equal(0, loaded.ActiveIndex);
        Assert.Equal(["a", "b"], loaded.Entries.Select(entry => entry.Query.SearchString));
        Assert.Equal(list.Entries[0].ResultFile, loaded.Entries[0].ResultFile);
        Assert.False(loaded.Entries[1].View.FixedFont);
    }

    private static ReopenWindow Window(string searchString, bool withResult)
    {
        SearchQuery query = new() { SearchString = searchString, FileNames = "*.cs", FolderName = @"c:\src" };
        SearchResult? result = withResult
            ? new SearchResult(query, [new FileResult(@"c:\src\a.cs", [new LineMatch(1, searchString, [new Hit(0, 1)])])],
                1, 1, 1, TimeSpan.Zero, false, [])
            : null;
        return new ReopenWindow(query, result, new ResultViewOptions());
    }
}
