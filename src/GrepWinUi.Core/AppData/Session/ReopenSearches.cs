using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;
using SaveAndLoad;

namespace GrepWinUi.Core.AppData.Session;

/// <summary>One result window to reopen: its query, its result file and its view options.</summary>
/// <param name="Query">Query of the window (may differ from the query of its result when it was edited later).</param>
/// <param name="ResultFile">Full path of the result file (<c>*.result.json</c>); empty for a window without result.</param>
/// <param name="View">View options of the window (header line, lines before / after).</param>
public sealed record ReopenEntry(SearchQuery Query, string ResultFile, ResultViewOptions View);

/// <summary>A result window as it is on exit: its query, its result (null without one) and its view options.</summary>
/// <param name="Query">Query of the window.</param>
/// <param name="Result">Shown result; null before the first search or while searching.</param>
/// <param name="View">View options of the window.</param>
public sealed record ReopenWindow(SearchQuery Query, SearchResult? Result, ResultViewOptions View);

/// <summary>
/// The result windows open on exit, reopened on the next start (Settings "Reopen last open searches": all,
/// "Show last search result": only the active one). This list (ReopenSearches.json) holds the windows in
/// their order; the result of each window is a file in the folder <see cref="FolderName"/>.
/// </summary>
public sealed class ReopenSearches : IStorable
{
    /// <inheritdoc/>
    public static string FileName => "ReopenSearches.json";

    /// <summary>Sub folder of the user folder with the window files.</summary>
    public const string FolderName = "Reopen";

    /// <summary>Windows in the order they were opened.</summary>
    public List<ReopenEntry> Entries { get; set; } = [];

    /// <summary>Index of the active window in <see cref="Entries"/>, -1 for none.</summary>
    public int ActiveIndex { get; set; } = -1;

    /// <summary>The active window's entry, null when <see cref="ActiveIndex"/> is out of range.</summary>
    public ReopenEntry? Active => ActiveIndex >= 0 && ActiveIndex < Entries.Count ? Entries[ActiveIndex] : null;

    /// <summary>
    /// Entries to open on start: all with <paramref name="reopenAll"/>, else only the active one with
    /// <paramref name="activeOnly"/>, else none.
    /// </summary>
    public IReadOnlyList<ReopenEntry> ToOpen(bool reopenAll, bool activeOnly) =>
        reopenAll ? Entries : activeOnly && Active is ReopenEntry active ? [active] : [];

    /// <summary>Name of the result file of window number <paramref name="index"/>: "Search_0.result.json".</summary>
    public static string WindowFileName(int index) => $"Search_{index}" + SearchResultFile.Extension;

    /// <summary>
    /// Writes the result of each window with a result into <paramref name="folder"/> (created if missing;
    /// old "Search_*.json" files there are deleted first). Returns the list to save. Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    /// <param name="folder">Folder of the window files (user folder + <see cref="FolderName"/>).</param>
    /// <param name="windows">The result windows in their order.</param>
    /// <param name="activeIndex">Index of the active window in <paramref name="windows"/>, -1 for none.</param>
    public static ReopenSearches WriteWindows(string folder, IReadOnlyList<ReopenWindow> windows, int activeIndex)
    {
        Directory.CreateDirectory(folder);
        foreach (string old in Directory.EnumerateFiles(folder, "Search_*.json"))
        {
            File.Delete(old);
        }

        ReopenSearches list = new() { ActiveIndex = activeIndex >= 0 && activeIndex < windows.Count ? activeIndex : -1 };
        for (int index = 0; index < windows.Count; index++)
        {
            ReopenWindow window = windows[index];
            string path = "";
            if (window.Result is SearchResult result)
            {
                path = Path.Combine(folder, WindowFileName(index));
                SearchResultFile.Save(result, path);
            }
            list.Entries.Add(new ReopenEntry(window.Query.Copy(), path, window.View with { }));
        }
        return list;
    }
}
