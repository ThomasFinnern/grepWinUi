using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using SaveAndLoad;

namespace GrepWinUi.Core.Tests;

public sealed class SearchQueryTests : IDisposable
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
    public void NewQuery_HasDocumentedDefaults()
    {
        SearchQuery query = new();

        Assert.Equal("", query.SearchString);
        Assert.False(query.RegularExpression);
        Assert.False(query.MatchCase);
        Assert.Equal("*.*", query.FileNames);
        Assert.Equal("", query.FolderName);
        Assert.True(query.IncludeSubfolders);
        Assert.Equal(RegexTarget.PathAndFileName, query.RegularExpressionOn);
        Assert.Equal(TimeUnit.Days, query.ModifiedWithinUnit);
        Assert.Null(query.ModifiedFrom);
        Assert.Equal(",", query.DelimitedSeparator);
        Assert.False(query.CountFilesFirst);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateDefault_TakesCountFilesFirstFromSettings(bool showProgressInPercent)
    {
        AppConfig config = new() { ShowProgressInPercent = showProgressInPercent };

        Assert.Equal(showProgressInPercent, SearchQuery.CreateDefault(config).CountFilesFirst);
    }

    [Fact]
    public void Copy_HasSameValuesButIsIndependent()
    {
        SearchQuery original = Sample();

        SearchQuery copy = original.Copy();
        Assert.Equal(Json(original), Json(copy));

        copy.SkippedFolders = "changed";
        Assert.Equal(".git bin obj", original.SkippedFolders);
    }

    [Fact]
    public void Comparer_EqualValues_AreEqualWithSameHash()
    {
        Assert.True(SearchQueryComparer.Instance.Equals(Sample(), Sample()));
        Assert.Equal(SearchQueryComparer.Instance.GetHashCode(Sample()), SearchQueryComparer.Instance.GetHashCode(Sample()));
    }

    [Fact]
    public void Comparer_OnlyOptionsDiffer_SameSearch()
    {
        SearchQuery other = Sample();
        other.SkipFolders = !other.SkipFolders;
        other.MatchCase = !other.MatchCase;

        Assert.True(SearchQueryComparer.Instance.Equals(Sample(), other));
    }

    [Fact]
    public void Comparer_FileAndFolderNamesIgnoreCase_SearchStringDoesNot()
    {
        SearchQuery upper = Sample();
        upper.FileNames = upper.FileNames.ToUpperInvariant();
        upper.FolderName = upper.FolderName.ToUpperInvariant();
        SearchQuery otherCase = Sample();
        otherCase.SearchString = "Function";

        Assert.True(SearchQueryComparer.Instance.Equals(Sample(), upper));
        Assert.Equal(SearchQueryComparer.Instance.GetHashCode(Sample()), SearchQueryComparer.Instance.GetHashCode(upper));
        Assert.False(SearchQueryComparer.Instance.Equals(Sample(), otherCase));
    }

    [Theory]
    [InlineData("SearchString")]
    [InlineData("FileNames")]
    [InlineData("FolderName")]
    public void Comparer_KeyValueDiffers_NotSameSearch(string property)
    {
        SearchQuery other = Sample();
        typeof(SearchQuery).GetProperty(property)!.SetValue(other, "different");

        Assert.False(SearchQueryComparer.Instance.Equals(Sample(), other));
        Assert.False(SearchQueryComparer.Instance.Equals(Sample(), null));
    }

    [Fact]
    public void SearchQueryList_SaveAndLoad_KeepsAllValues()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "Defaults")));
        SearchQueryList list = new();
        list.Add(Sample());

        store.Save(list);
        LoadResult<SearchQueryList> loaded = store.Load<SearchQueryList>();

        Assert.Equal(Json(Sample()), Json(Assert.Single(loaded.Value.Items)));
        Assert.Contains("\"PathName\"", File.ReadAllText(store.UserFilePath<SearchQueryList>()));
    }

    [Fact]
    public void SearchQueryList_SameSearchWithNewOptions_ReplacesOldEntryOnTop()
    {
        SearchQueryList list = new();
        list.Add(Sample());
        list.Add(new SearchQuery { SearchString = "other" });
        SearchQuery newer = Sample();
        newer.IncludeSubfolders = false;

        list.Add(newer);

        Assert.Equal(2, list.Items.Count);
        Assert.Same(newer, list.Items[0]);
    }

    [Fact]
    public void SearchStringList_CaseMatters()
    {
        SearchStringList list = new();
        list.Add("Foo");
        list.Add("foo");

        Assert.Equal(["foo", "Foo"], list.Items);
    }

    [Fact]
    public void FileNamesAndFolderLists_IgnoreCase()
    {
        FileNamesList files = new();
        files.Add("*.CS");
        files.Add("*.cs");
        FolderNameList folders = new();
        folders.Add(@"C:\Temp");
        folders.Add(@"c:\temp");

        Assert.Equal(["*.cs"], files.Items);
        Assert.Equal([@"c:\temp"], folders.Items);
    }

    [Fact]
    public void Lists_HaveStartMaxCounts()
    {
        Assert.Equal(30, new SearchStringList().MaxCount);
        Assert.Equal(15, new SearchStringList().MaxFixedCount);
        Assert.Equal(30, new FileNamesList().MaxCount);
        Assert.Equal(15, new FileNamesList().MaxFixedCount);
        Assert.Equal(40, new FolderNameList().MaxCount);
        Assert.Equal(15, new SearchQueryList().MaxCount);
    }

    private static string Json(SearchQuery query) => System.Text.Json.JsonSerializer.Serialize(query);

    private static SearchQuery Sample() => new()
    {
        SearchString = "function",
        RegularExpression = true,
        FileNames = "*controller.php",
        FolderName = @"e:\wamp64\www\api_6x",
        FolderNamesAsRegex = true,
        RegularExpressionOn = RegexTarget.PathName,
        ModifiedBetween = true,
        ModifiedFrom = new DateTime(2026, 1, 1),
        SkipFolders = true,
        SkippedFolders = ".git bin obj",
    };
}
