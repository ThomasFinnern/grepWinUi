using GrepWinUi.Core.AppData.Config;
using SaveAndLoad;

namespace GrepWinUi.Core.AppData.Search;

/// <summary>The four recent-first lists of the search form, loaded and saved together.</summary>
public sealed class SearchLists
{
    /// <summary>Last used search strings.</summary>
    public SearchStringList SearchStrings { get; init; } = new();

    /// <summary>Last used file name patterns.</summary>
    public FileNamesList FileNames { get; init; } = new();

    /// <summary>Last used folder names.</summary>
    public FolderNameList FolderNames { get; init; } = new();

    /// <summary>Last used complete queries (newest on top).</summary>
    public SearchQueryList Queries { get; init; } = new();

    /// <summary>Loads all lists; never throws. Problems (e.g. broken files) are collected in <paramref name="problems"/>.</summary>
    public static SearchLists Load(ObjectStore store, out IReadOnlyList<string> problems)
    {
        List<string> found = [];
        SearchLists lists = new()
        {
            SearchStrings = Value(store.Load<SearchStringList>(), found),
            FileNames = Value(store.Load<FileNamesList>(), found),
            FolderNames = Value(store.Load<FolderNameList>(), found),
            Queries = Value(store.Load<SearchQueryList>(), found),
        };
        problems = found;
        return lists;
    }

    /// <summary>Saves all lists. Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>.</summary>
    public void Save(ObjectStore store)
    {
        store.Save(SearchStrings);
        store.Save(FileNames);
        store.Save(FolderNames);
        store.Save(Queries);
    }

    /// <summary>
    /// Puts the non-empty search string, file names and folder name of <paramref name="query"/> on top
    /// of their lists; with <paramref name="asPastQuery"/> also a copy of the whole query.
    /// </summary>
    public void Remember(SearchQuery query, bool asPastQuery)
    {
        AddIfNotEmpty(SearchStrings, query.SearchString);
        AddIfNotEmpty(FileNames, query.FileNames);
        AddIfNotEmpty(FolderNames, query.FolderName);
        if (asPastQuery)
        {
            Queries.Add(query.Copy());
        }
    }

    /// <summary>Applies the list limits of the settings (at start and after the settings changed).</summary>
    public void ApplySettings(AppConfig config)
    {
        Apply(SearchStrings, config.SearchStringTokens);
        Apply(FileNames, config.FileNameTokens);
    }

    /// <summary>Dropdown items of the search string: used and fixed tokens merged as set in the settings.</summary>
    public IReadOnlyList<string> SearchStringTokens(AppConfig config) =>
        SearchStrings.Merged(config.SearchStringTokens.PreFixedCount, config.SearchStringTokens.FirstFixedCount);

    /// <summary>Dropdown items of the file names: used and fixed tokens merged as set in the settings.</summary>
    public IReadOnlyList<string> FileNameTokens(AppConfig config) =>
        FileNames.Merged(config.FileNameTokens.PreFixedCount, config.FileNameTokens.FirstFixedCount);

    private static void Apply(RecentLists.PinnedRecentList<string> list, TokenListOptions options)
    {
        list.MaxCount = Math.Max(1, options.MaxCount);
        list.MaxFixedCount = Math.Max(0, options.MaxFixedCount);
    }

    /// <summary>Query for a new result form: a copy of the newest past query, else the defaults.</summary>
    public SearchQuery StartQuery(AppConfig config) =>
        Queries.Items.Count > 0 ? Queries.Items[0].Copy() : SearchQuery.CreateDefault(config);

    private static void AddIfNotEmpty(RecentLists.RecentList<string> list, string value)
    {
        if (value.Trim().Length > 0)
        {
            list.Add(value);
        }
    }

    private static T Value<T>(LoadResult<T> result, List<string> problems)
    {
        if (result.Problem is not null)
        {
            problems.Add(result.Problem);
        }
        return result.Value;
    }
}
