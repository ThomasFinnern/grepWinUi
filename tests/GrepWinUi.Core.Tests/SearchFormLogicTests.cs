using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using SaveAndLoad;

namespace GrepWinUi.Core.Tests;

/// <summary>Logic behind the search properties dialog: lists, start query, titles, dropdowns, "[+]" state, line limits.</summary>
public sealed class SearchFormLogicTests : IDisposable
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
    public void StartQuery_EmptyList_DefaultsWithCountFilesFirstFromSettings()
    {
        SearchQuery query = new SearchLists().StartQuery(new AppConfig { ShowProgressInPercent = true });

        Assert.Equal("", query.SearchString);
        Assert.True(query.CountFilesFirst);
    }

    [Fact]
    public void StartQuery_CopyOfNewestPastQuery()
    {
        SearchLists lists = new();
        lists.Remember(new SearchQuery { SearchString = "old" }, asPastQuery: true);
        lists.Remember(new SearchQuery { SearchString = "new" }, asPastQuery: true);

        SearchQuery query = lists.StartQuery(new AppConfig());

        Assert.Equal("new", query.SearchString);
        Assert.NotSame(lists.Queries.Items[0], query);
    }

    [Fact]
    public void Remember_AddsNonEmptyTextsToTheirLists()
    {
        SearchLists lists = new();

        lists.Remember(new SearchQuery { SearchString = "function", FileNames = "*.php", FolderName = " " }, asPastQuery: false);

        Assert.Equal(["function"], lists.SearchStrings.Items);
        Assert.Equal(["*.php"], lists.FileNames.Items);
        Assert.Empty(lists.FolderNames.Items);
        Assert.Empty(lists.Queries.Items);
    }

    [Fact]
    public void Remember_AsPastQuery_StoresACopy()
    {
        SearchLists lists = new();
        SearchQuery query = new() { SearchString = "function" };

        lists.Remember(query, asPastQuery: true);
        query.MatchCase = true;

        Assert.False(Assert.Single(lists.Queries.Items).MatchCase);
    }

    [Fact]
    public void SaveAndLoad_KeepsAllFourLists()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "Defaults")));
        SearchLists lists = new();
        lists.Remember(new SearchQuery { SearchString = "s", FileNames = "*.cs", FolderName = @"c:\src" }, asPastQuery: true);

        lists.Save(store);
        SearchLists loaded = SearchLists.Load(store, out IReadOnlyList<string> problems);

        Assert.Empty(problems);
        Assert.Equal(["s"], loaded.SearchStrings.Items);
        Assert.Equal(["*.cs"], loaded.FileNames.Items);
        Assert.Equal([@"c:\src"], loaded.FolderNames.Items);
        Assert.Equal("s", Assert.Single(loaded.Queries.Items).SearchString);
    }

    [Fact]
    public void Load_BrokenFile_ReportsProblemAndUsesEmptyList()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "Defaults")));
        Directory.CreateDirectory(Path.Combine(_root, "User"));
        File.WriteAllText(store.UserFilePath<SearchStringList>(), "{ not json");

        SearchLists loaded = SearchLists.Load(store, out IReadOnlyList<string> problems);

        Assert.Single(problems);
        Assert.Empty(loaded.SearchStrings.Items);
    }

    [Fact]
    public void Title_SearchStringFileNamesFolder()
    {
        SearchQuery query = new() { SearchString = "function", FileNames = "*controller.php", FolderName = @"e:\www\api" };

        Assert.Equal(@"function :: *controller.php :: e:\www\api", SearchQueryText.Title(query));
    }

    [Fact]
    public void Title_LongFolder_ShortenedToItsEnd()
    {
        string folder = @"e:\wamp64\www\api_6x\api\components\very\deep";
        SearchQuery query = new() { SearchString = "x", FileNames = "*.*", FolderName = folder };

        string title = SearchQueryText.Title(query);

        Assert.EndsWith(" :: ..." + folder[^30..], title);
    }

    [Fact]
    public void DropDownItems_CurrentFirstWithoutDuplicate()
    {
        IReadOnlyList<string> items = SearchQueryText.DropDownItems("*.CS", ["*.md", "*.cs", "*.txt"], StringComparer.OrdinalIgnoreCase);

        Assert.Equal(["*.CS", "*.md", "*.txt"], items);
    }

    [Fact]
    public void DropDownItems_EmptyCurrent_OnlyListItems()
    {
        Assert.Equal(["a", "b"], SearchQueryText.DropDownItems("", ["a", "b"], StringComparer.Ordinal));
    }

    [Fact]
    public void SectionOptions_DefaultsClosed_ChangedOptionOpens()
    {
        Assert.False(SearchQueryText.HasFileNameOptions(new SearchQuery()));
        Assert.False(SearchQueryText.HasFolderOptions(new SearchQuery()));
        Assert.True(SearchQueryText.HasFileNameOptions(new SearchQuery { SkipByExtension = true }));
        Assert.True(SearchQueryText.HasFolderOptions(new SearchQuery { IncludeSubfolders = false }));
    }

    [Fact]
    public void LimitShowToKeep_ShowNotAboveKeep()
    {
        ResultViewOptions view = new() { KeepLinesBefore = 2, ShowLinesBefore = 5, KeepLinesAfter = 4, ShowLinesAfter = 3 };

        view.LimitShowToKeep();

        Assert.Equal(2, view.ShowLinesBefore);
        Assert.Equal(3, view.ShowLinesAfter);
    }
}
