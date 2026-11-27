using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.AppData.Session;
using GrepWinUi.Core.Search;
using SaveAndLoad;

namespace GrepWinUi.Core.Tests;

/// <summary>File → Last used search files: the list, loading a file of unknown kind, the form with the same search.</summary>
public sealed class RecentFilesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public RecentFilesTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void List_NewestOnTopIgnoresCaseMax15()
    {
        RecentFilesList list = new();
        for (int index = 1; index <= 16; index++)
        {
            list.Add($@"c:\q\{index}.json");
        }
        list.Add(@"C:\Q\5.JSON");

        Assert.Equal(15, list.Items.Count);
        Assert.Equal(@"C:\Q\5.JSON", list.Items[0]);
        Assert.Single(list.Items, path => string.Equals(path, @"c:\q\5.json", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(@"c:\q\1.json", list.Items);
    }

    [Fact]
    public void List_SaveAndLoad()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "user"), Path.Combine(_root, "defaults")));
        RecentFilesList list = new();
        list.Add(@"c:\a.json");
        list.Add(@"c:\b.result.json");

        store.Save(list);
        RecentFilesList loaded = store.Load<RecentFilesList>().Value;

        Assert.Equal([@"c:\b.result.json", @"c:\a.json"], loaded.Items);
        Assert.True(File.Exists(Path.Combine(_root, "user", RecentFilesList.FileName)));
    }

    [Fact]
    public void Loader_ResultFile_QueryAndResult()
    {
        string path = Path.Combine(_root, "r.result.json");
        SearchResult result = new(Query("foo"), [new FileResult(@"c:\a.cs", [new LineMatch(1, "foo", [new Hit(0, 3)])])],
            1, 1, 1, TimeSpan.Zero, false, []);
        SearchResultFile.Save(result, path);

        LoadedSearchFile loaded = SearchFileLoader.Load(path);

        Assert.NotNull(loaded.Result);
        Assert.Equal("foo", loaded.Query.SearchString);
        Assert.Single(loaded.Result.Files);
    }

    [Fact]
    public void Loader_QueryFile_QueryOnly()
    {
        string path = Path.Combine(_root, "q.json");
        SearchQueryFile.Save(Query("bar"), path);

        LoadedSearchFile loaded = SearchFileLoader.Load(path);

        Assert.Null(loaded.Result);
        Assert.Equal("bar", loaded.Query.SearchString);
    }

    [Fact]
    public void Loader_KindFromHeaderNotExtension()
    {
        string path = Path.Combine(_root, "named-like-a-query.json");
        SearchResultFile.Save(new SearchResult(Query("x"), [], 0, 0, 0, TimeSpan.Zero, false, []), path);

        Assert.NotNull(SearchFileLoader.Load(path).Result);
    }

    [Fact]
    public void Loader_OtherJson_InvalidData()
    {
        string path = Path.Combine(_root, "other.json");
        File.WriteAllText(path, """{"Items":[]}""");

        InvalidDataException ex = Assert.Throws<InvalidDataException>(() => SearchFileLoader.Load(path));
        Assert.Contains("neither", ex.Message);
    }

    [Fact]
    public void Loader_MissingFile_IOException()
    {
        Assert.ThrowsAny<IOException>(() => SearchFileLoader.Load(Path.Combine(_root, "missing.json")));
    }

    [Fact]
    public void FindSame_SameSearchStringFilesFolder_OptionsIgnored()
    {
        SearchQuery first = Query("foo");
        SearchQuery second = Query("bar");
        SearchQuery wanted = Query("foo");
        wanted.MatchCase = true;
        wanted.FolderName = @"C:\SRC";

        Assert.Same(first, SearchQueryComparer.FindSame([second, first], query => query, wanted));
        Assert.Null(SearchQueryComparer.FindSame([second], query => query, wanted));
    }

    private static SearchQuery Query(string searchString) =>
        new() { SearchString = searchString, FileNames = "*.cs", FolderName = @"c:\src" };
}
