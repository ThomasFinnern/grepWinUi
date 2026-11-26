using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Result modes of a search query (search string options of step 24) and the texts that depend on them.
/// </summary>
/// <param name="FirstMatchOnly">"First match per file only": keep the first found line of each file.</param>
/// <param name="NonMatchingLines">"Non matching lines": the lines without a match are the found lines.</param>
/// <param name="FilesWithoutMatch">"List files without match": only files without a found line, no lines.</param>
/// <param name="FileNamesOnly">"File names only (no lines)": files with a found line, no lines (like grep -l).</param>
public readonly record struct ResultMode(bool FirstMatchOnly, bool NonMatchingLines, bool FilesWithoutMatch, bool FileNamesOnly)
{
    /// <summary>Modes of <paramref name="query"/>.</summary>
    public static ResultMode Of(SearchQuery query) =>
        new(query.FirstMatchPerFileOnly, query.NonMatchingLines, query.ListFilesWithoutMatch, query.FileNamesOnly);

    /// <summary>True when the result keeps no lines (files without match, or file names only).</summary>
    public bool NoLines => FilesWithoutMatch || FileNamesOnly;

    /// <summary>
    /// Counts of a result for the summary and the status bar, e.g. "12 matches in 3 files from 57 files searched",
    /// "120 lines without match in 7 files from …", "12 files without match from …", "12 files with matches from …".
    /// </summary>
    public static string Counts(SearchResult result)
    {
        ResultMode mode = Of(result.Query);
        string searched = $"from {result.FilesSearched} files searched";
        return mode.FilesWithoutMatch ? $"{result.Files.Count} files without match {searched}"
            : mode.FileNamesOnly ? $"{result.Files.Count} files with matches {searched}"
            : mode.NonMatchingLines ? $"{result.MatchCount} lines without match in {result.Files.Count} files {searched}"
            : $"{result.MatchCount} matches in {result.Files.Count} files {searched}";
    }

    /// <summary>Count of a file for <see cref="SearchResult.MatchCount"/>: hits, or found lines with "Non matching lines".</summary>
    public int CountOf(FileResult file) =>
        NonMatchingLines ? file.Lines.Count : file.Lines.Sum(line => line.Hits.Count);
}
