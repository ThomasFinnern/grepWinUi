using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Which files the next search of a result form reads: the folders of the query, or with
/// "Only in files of current result" the files of the form's current result.
/// </summary>
/// <param name="OnlyFiles">Files to search instead of the folders; null = search the folders.</param>
/// <param name="Problem">Note for the result when the files of the current result were asked for but there are none.</param>
public sealed record SearchScope(IReadOnlyList<string>? OnlyFiles, string? Problem)
{
    /// <summary>Note when "Only in files of current result" falls back to the folders.</summary>
    public const string NoCurrentFilesProblem = "Only in files of current result: there is no current result with files, so the folder was searched.";

    /// <summary>
    /// The files of <paramref name="current"/> when <paramref name="query"/> asks for "Only in files of current
    /// result" and it has files; else the folders, with <see cref="NoCurrentFilesProblem"/> when the option is set
    /// (old program: same fallback, without a note).
    /// </summary>
    public static SearchScope Of(SearchQuery query, SearchResult? current)
    {
        if (!query.OnlyInFilesOfCurrentResult)
        {
            return new SearchScope(null, null);
        }
        return current is { Files.Count: > 0 }
            ? new SearchScope([.. current.Files.Select(file => file.Path)], null)
            : new SearchScope(null, NoCurrentFilesProblem);
    }
}
