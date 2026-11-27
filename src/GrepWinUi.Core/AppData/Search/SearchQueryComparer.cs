namespace GrepWinUi.Core.AppData.Search;

/// <summary>
/// Two queries are the same search when search string, file names and folder name are equal
/// (the parts the user sees in the abbreviated list). Options are not compared, so the newest
/// options of a search replace the older ones in a recent list.
/// Search string: case matters; file and folder names: case is ignored, as in Windows.
/// </summary>
public sealed class SearchQueryComparer : IEqualityComparer<SearchQuery>
{
    /// <summary>Shared instance.</summary>
    public static SearchQueryComparer Instance { get; } = new();

    /// <inheritdoc/>
    public bool Equals(SearchQuery? x, SearchQuery? y) =>
        ReferenceEquals(x, y)
        || (x is not null && y is not null
            && string.Equals(x.SearchString, y.SearchString, StringComparison.Ordinal)
            && string.Equals(x.FileNames, y.FileNames, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.FolderName, y.FolderName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// First of <paramref name="items"/> whose query (<paramref name="queryOf"/>) is the same search as
    /// <paramref name="query"/>, else default (e.g. the open result form showing that search).
    /// </summary>
    public static T? FindSame<T>(IEnumerable<T> items, Func<T, SearchQuery> queryOf, SearchQuery query) where T : class =>
        items.FirstOrDefault(item => Instance.Equals(queryOf(item), query));

    /// <inheritdoc/>
    public int GetHashCode(SearchQuery obj) => HashCode.Combine(
        StringComparer.Ordinal.GetHashCode(obj.SearchString),
        StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FileNames),
        StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FolderName));
}
