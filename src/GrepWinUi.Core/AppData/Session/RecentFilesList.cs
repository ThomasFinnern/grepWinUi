using RecentLists;
using SaveAndLoad;

namespace GrepWinUi.Core.AppData.Session;

/// <summary>
/// File → Last used search files: search query and search result files, newest on top, as in the old
/// program one list for both kinds. A file is added when it is saved, opened or loaded.
/// Case of the paths is ignored, as in Windows. Saved as RecentFiles.json.
/// </summary>
public sealed class RecentFilesList : RecentList<string>, IStorable
{
    /// <inheritdoc/>
    public static string FileName => "RecentFiles.json";

    /// <summary>Empty list, up to 15 files.</summary>
    public RecentFilesList() : base(comparer: StringComparer.OrdinalIgnoreCase) { }
}
