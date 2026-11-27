using GrepWinUi.Core;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.App;

/// <summary>Main window: File → Last used search files and File → Last used search queries.</summary>
public sealed partial class MainForm
{
    /// <summary>Fills File → Last used search files with the full paths, newest on top ("(empty)" without files).</summary>
    private void FillRecentFilesMenu(ToolStripMenuItem menu) =>
        FillListMenu(menu, _recentFiles.Items, path => path, OpenRecentFile);

    /// <summary>
    /// Fills File → Last used search queries from the past queries (newest on top), each shown as
    /// "search string :: file names :: folder". A click opens a copy of the query with
    /// <see cref="OpenQuery"/> (form with the same search or a new one, then the search properties dialog).
    /// </summary>
    private void FillRecentQueriesMenu(ToolStripMenuItem menu) =>
        FillListMenu(menu, _lists.Queries.Items, SearchQueryText.Title, query => OpenQuery(query.Copy()));

    /// <summary>Submenu with one item per list entry (text shown as is), or "(empty)" for an empty list.</summary>
    private static void FillListMenu<T>(ToolStripMenuItem menu, IEnumerable<T> entries, Func<T, string> text, Action<T> open)
    {
        menu.DropDownItems.Clear();
        foreach (T entry in entries.ToList())
        {
            ToolStripMenuItem item = new(MenuText.Literal(text(entry)));
            item.Click += (_, _) => open(entry);
            menu.DropDownItems.Add(item);
        }
        if (menu.DropDownItems.Count == 0)
        {
            menu.DropDownItems.Add(MainMenu.EmptyListItem());
        }
    }

    /// <summary>
    /// Opens a file of File → Last used search files: a result file like File → Open search result, a query
    /// file like File → Open search query (both use a form that already shows the same search).
    /// A missing file is reported and removed from the list.
    /// </summary>
    private void OpenRecentFile(string path)
    {
        if (!File.Exists(path))
        {
            MessageBox.Show(this, $"File not found:\n{path}\n\nIt is removed from the list.", "Last used search files",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _recentFiles.Remove(path);
            SaveRecentFiles();
            return;
        }

        LoadedSearchFile file;
        try
        {
            file = SearchFileLoader.Load(path);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Search file not loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (file.Result is SearchResult result)
        {
            RememberResultFile(path);
            ShowResult(result);
        }
        else
        {
            RememberQueryFile(path);
            OpenQuery(file.Query);
        }
    }

    /// <summary>Puts <paramref name="path"/> on top of File → Last used search files and saves the list.</summary>
    private void AddRecentFile(string path)
    {
        _recentFiles.Add(path);
        SaveRecentFiles();
    }

    /// <summary>Saves <c>RecentFiles.json</c> (problems are ignored: only the menu list is lost).</summary>
    private void SaveRecentFiles()
    {
        try
        {
            _store.Save(_recentFiles);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Only the list of File → Last used search files is lost.
        }
    }
}
