using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Texts of the main window's status bar for one result form (see "UI spec: status bar" in CLAUDE.md).
/// The status bar shows the status of the active result form.
/// </summary>
/// <param name="Title">Short info, e.g. "Searching 'foo'" or "Search done".</param>
/// <param name="Info">Longer info: the file being read (if enabled) or the result summary.</param>
/// <param name="Folder">Last name of the folder being searched, e.g. "api_6x".</param>
/// <param name="Files">Files read / all files, e.g. "12 / ?" (all not known), "12 / 57" or "57 / 57" at the end.</param>
/// <param name="Time">Elapsed time, e.g. "00:00:07".</param>
/// <param name="IsRunning">True while the search runs (Working field animates).</param>
/// <param name="Percent">Files done in % of all files, when all files were counted first; else null.</param>
public sealed record SearchStatus(
    string Title, string Info, string Folder, string Files, string Time, bool IsRunning, int? Percent = null)
{
    /// <summary>Text for a number of files not known (files not counted first, or the search was cancelled).</summary>
    public const string UnknownCount = "?";

    /// <summary>No search yet (or no result form): all fields empty.</summary>
    public static SearchStatus Idle { get; } = new("", "", "", "", "", IsRunning: false);

    /// <summary>
    /// Status while <paramref name="query"/> is searched. With "Count files first": first
    /// "Counting files for '…'" (Files = files counted so far / ?), then Files = read / all and a percent.
    /// </summary>
    /// <param name="query">The running query.</param>
    /// <param name="progress">Last progress report; null right after the start.</param>
    /// <param name="elapsed">Time since the start.</param>
    /// <param name="showFileNames">Settings "Show file names in status bar": Info shows the file being read.</param>
    public static SearchStatus Running(SearchQuery query, SearchProgress? progress, TimeSpan elapsed, bool showFileNames)
    {
        string currentFile = progress?.CurrentFile ?? "";
        string folder = currentFile.Length > 0 ? Path.GetDirectoryName(currentFile) ?? "" : query.FolderName;
        bool counting = progress?.Counting ?? query.CountFilesFirst;
        int? total = counting ? null : progress?.TotalFiles;
        int read = progress?.FilesSearched ?? 0;
        string files = counting
            ? $"{progress?.TotalFiles ?? 0} / {UnknownCount}"
            : $"{read} / {total?.ToString() ?? UnknownCount}";
        return new SearchStatus(
            counting ? $"Counting files for '{query.SearchString}'" : $"Searching '{query.SearchString}'",
            showFileNames ? currentFile : "",
            LastFolderName(folder),
            files,
            TimeText(elapsed),
            IsRunning: true,
            total is int all ? PercentOf(read, all) : null);
    }

    /// <summary>Done files in % of all files (0..100); 100 when there are no files.</summary>
    public static int PercentOf(int done, int all) =>
        all <= 0 ? 100 : (int)Math.Clamp(done * 100L / all, 0, 100);

    /// <summary>
    /// Status of a finished (or loaded) result: "Search done" / "Search cancelled", the summary and
    /// the search folder. All files are known only when the search was not cancelled.
    /// </summary>
    public static SearchStatus Done(SearchResult result)
    {
        string all = result.Cancelled ? UnknownCount : result.FilesSearched.ToString();
        return new SearchStatus(
            result.Cancelled ? "Search cancelled" : "Search done",
            ResultMode.Counts(result),
            LastFolderName(result.Query.FolderName),
            $"{result.FilesSearched} / {all}",
            TimeText(result.Duration),
            IsRunning: false);
    }

    /// <summary>Status after an unexpected error of the search.</summary>
    public static SearchStatus Failed(SearchQuery query, string message, TimeSpan elapsed) =>
        new("Search failed", message, LastFolderName(query.FolderName), "", TimeText(elapsed), IsRunning: false);

    /// <summary>
    /// Last name of a folder path: <c>e:\wamp64\www\api_6x</c> → <c>api_6x</c> (also with a trailing
    /// backslash); a drive root like <c>e:\</c> stays as it is.
    /// </summary>
    public static string LastFolderName(string folder)
    {
        string trimmed = folder.TrimEnd('\\', '/');
        string name = Path.GetFileName(trimmed);
        return name.Length > 0 ? name : folder;
    }

    /// <summary>Elapsed time as hours:minutes:seconds, e.g. "00:00:07" (hours can grow beyond 24).</summary>
    public static string TimeText(TimeSpan elapsed) =>
        $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
}
