using RecentLists;
using SaveAndLoad;

namespace GrepWinUi.Core.AppData.Search;

/// <summary>Last used search strings (dropdown of the search string). Case matters.</summary>
public sealed class SearchStringList : RecentList<string>, IStorable
{
    /// <inheritdoc/>
    public static string FileName => "SearchStrings.json";

    /// <summary>Empty list, up to 35 items.</summary>
    public SearchStringList() : base(maxCount: 35, comparer: StringComparer.Ordinal) { }
}

/// <summary>Last used file name patterns (dropdown of the file names). Case is ignored, as in Windows.</summary>
public sealed class FileNamesList : RecentList<string>, IStorable
{
    /// <inheritdoc/>
    public static string FileName => "FileNames.json";

    /// <summary>Empty list, up to 35 items.</summary>
    public FileNamesList() : base(maxCount: 35, comparer: StringComparer.OrdinalIgnoreCase) { }
}

/// <summary>Last used folder names (dropdown of the folder name). Case is ignored, as in Windows.</summary>
public sealed class FolderNameList : RecentList<string>, IStorable
{
    /// <inheritdoc/>
    public static string FileName => "FolderNames.json";

    /// <summary>Empty list, up to 40 items.</summary>
    public FolderNameList() : base(maxCount: 40, comparer: StringComparer.OrdinalIgnoreCase) { }
}

/// <summary>Last used complete search queries (File → Last used search queries).</summary>
public sealed class SearchQueryList : RecentList<SearchQuery>, IStorable
{
    /// <inheritdoc/>
    public static string FileName => "SearchQueries.json";

    /// <summary>Empty list, up to 15 items; equal queries (all values) count as one.</summary>
    public SearchQueryList() : base(comparer: SearchQueryComparer.Instance) { }
}
