using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>A loaded search file: a search result (with its query) or only a search query.</summary>
/// <param name="Query">Query of the file.</param>
/// <param name="Result">The result of a result file; null for a query file.</param>
public sealed record LoadedSearchFile(SearchQuery Query, SearchResult? Result);

/// <summary>
/// Loads a file of File → Last used search files without knowing its kind (as in the old program):
/// first as search result (<see cref="SearchResultFile"/>), then as search query (<see cref="SearchQueryFile"/>).
/// The kind is taken from the file header, not from the extension.
/// </summary>
public static class SearchFileLoader
{
    /// <summary>Reads a search result or search query file.</summary>
    /// <exception cref="InvalidDataException">The file is neither a grepWinUi search result nor a search query.</exception>
    /// <exception cref="IOException">The file can't be read (e.g. it doesn't exist).</exception>
    /// <exception cref="UnauthorizedAccessException">No read access.</exception>
    public static LoadedSearchFile Load(string path)
    {
        try
        {
            SearchResult result = SearchResultFile.Load(path);
            return new LoadedSearchFile(result.Query, result);
        }
        catch (InvalidDataException)
        {
            // Not a result file: try a query file next.
        }

        try
        {
            return new LoadedSearchFile(SearchQueryFile.Load(path), null);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"'{path}' is neither a grepWinUi search result nor a search query file.", ex);
        }
    }
}
