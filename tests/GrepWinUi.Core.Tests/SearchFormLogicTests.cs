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
    public void ApplySettings_SetsSearchStringLimits()
    {
        SearchLists lists = new();
        lists.SearchStrings.Fixed.AddRange(["f1", "f2", "f3"]);
        AppConfig config = new();
        config.SearchStringTokens.MaxCount = 2;
        config.SearchStringTokens.MaxFixedCount = 1;

        lists.ApplySettings(config);
        lists.Remember(new SearchQuery { SearchString = "a" }, asPastQuery: false);
        lists.Remember(new SearchQuery { SearchString = "b" }, asPastQuery: false);
        lists.Remember(new SearchQuery { SearchString = "c" }, asPastQuery: false);

        Assert.Equal(["c", "b"], lists.SearchStrings.Items);
        Assert.Equal(["f1"], lists.SearchStrings.Fixed);
    }

    [Fact]
    public void Remember_FixedSearchStringStaysInPlace()
    {
        SearchLists lists = new();
        lists.SearchStrings.Fixed.AddRange(["f1", "f2"]);

        lists.Remember(new SearchQuery { SearchString = "f2" }, asPastQuery: false);

        Assert.Equal(["f1", "f2"], lists.SearchStrings.Fixed);
    }

    [Fact]
    public void SearchStringTokens_MergedAsInSettings()
    {
        SearchLists lists = new();
        lists.SearchStrings.Items.AddRange(["u1", "u2", "u3"]);
        lists.SearchStrings.Fixed.AddRange(["f1", "f2"]);
        AppConfig config = new();
        config.SearchStringTokens.PreFixedCount = 1;
        config.SearchStringTokens.FirstFixedCount = 1;

        Assert.Equal(["u1", "f1", "u2", "u3", "f2"], lists.SearchStringTokens(config));
    }

    [Fact]
    public void ApplySettings_FileNameLimitsIndependentOfSearchStrings()
    {
        SearchLists lists = new();
        AppConfig config = new();
        config.FileNameTokens.MaxCount = 1;
        config.SearchStringTokens.MaxCount = 5;

        lists.ApplySettings(config);

        Assert.Equal(1, lists.FileNames.MaxCount);
        Assert.Equal(5, lists.SearchStrings.MaxCount);
    }

    [Fact]
    public void FileNameTokens_MergedAsInSettings_FixedStayOnRemember()
    {
        SearchLists lists = new();
        lists.FileNames.Fixed.AddRange(["*.cs", "*.md"]);
        AppConfig config = new();
        config.FileNameTokens.PreFixedCount = 1;
        config.FileNameTokens.FirstFixedCount = 1;

        lists.Remember(new SearchQuery { FileNames = "*.php" }, asPastQuery: false);
        lists.Remember(new SearchQuery { FileNames = "*.MD" }, asPastQuery: false);

        Assert.Equal(["*.cs", "*.md"], lists.FileNames.Fixed);
        Assert.Equal(["*.MD", "*.cs", "*.php"], lists.FileNameTokens(config));
    }

    [Fact]
    public void FolderNameTokens_LimitsAndMergeFromFolderSettings()
    {
        SearchLists lists = new();
        lists.FolderNames.Fixed.AddRange([@"c:\fixed1", @"c:\fixed2"]);
        AppConfig config = new();
        config.FolderNameTokens.MaxCount = 2;
        config.FolderNameTokens.PreFixedCount = 1;
        config.FolderNameTokens.FirstFixedCount = 1;
        lists.ApplySettings(config);

        lists.Remember(new SearchQuery { FolderName = @"c:\a" }, asPastQuery: false);
        lists.Remember(new SearchQuery { FolderName = @"c:\b" }, asPastQuery: false);
        lists.Remember(new SearchQuery { FolderName = @"C:\B" }, asPastQuery: false);
        lists.Remember(new SearchQuery { FolderName = @"c:\c" }, asPastQuery: false);

        Assert.Equal([@"c:\c", @"C:\B"], lists.FolderNames.Items);
        Assert.Equal([@"c:\c", @"c:\fixed1", @"C:\B", @"c:\fixed2"], lists.FolderNameTokens(config));
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
