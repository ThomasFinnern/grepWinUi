namespace GrepWinUi.Core.AppData.Search;

/// <summary>Texts and checks the search form and result form need about a <see cref="SearchQuery"/>.</summary>
public static class SearchQueryText
{
    /// <summary>Folder names longer than this are shortened in the title.</summary>
    public const int MaxTitleFolderLength = 32;

    /// <summary>
    /// Short title as in the old program: "search string :: file names :: folder".
    /// A long folder is shortened to "..." + its last 30 characters.
    /// </summary>
    public static string Title(SearchQuery query)
    {
        string folder = query.FolderName.Length > MaxTitleFolderLength
            ? "..." + query.FolderName[^(MaxTitleFolderLength - 2)..]
            : query.FolderName;
        return $"{query.SearchString} :: {query.FileNames} :: {folder}";
    }

    /// <summary>
    /// Dropdown items: the current value first (if not empty), then the list items without it.
    /// </summary>
    public static IReadOnlyList<string> DropDownItems(string current, IEnumerable<string> items, StringComparer comparer)
    {
        List<string> result = current.Length > 0 ? [current] : [];
        foreach (string item in items)
        {
            if (!result.Contains(item, comparer))
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>True when an option of the file names section differs from its default (its "[+]" starts open).</summary>
    public static bool HasFileNameOptions(SearchQuery query)
    {
        SearchQuery d = new();
        return query.FileNamesAsRegex != d.FileNamesAsRegex
            || query.SearchInsideZipArchives != d.SearchInsideZipArchives
            || query.SkipBinaryFiles != d.SkipBinaryFiles
            || query.OnlyBinaryFiles != d.OnlyBinaryFiles
            || query.SkipByExtension != d.SkipByExtension;
    }

    /// <summary>True when an option of the folder section differs from its default (its "[+]" starts open).</summary>
    public static bool HasFolderOptions(SearchQuery query)
    {
        SearchQuery d = new();
        return query.IncludeSubfolders != d.IncludeSubfolders
            || query.FolderNamesAsRegex != d.FolderNamesAsRegex
            || query.BaseNameAsRegex != d.BaseNameAsRegex
            || query.SkipFolders != d.SkipFolders;
    }
}
