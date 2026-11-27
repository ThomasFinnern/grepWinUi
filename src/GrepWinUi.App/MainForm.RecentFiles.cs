using GrepWinUi.Core.Search;

namespace GrepWinUi.App;

/// <summary>Main window: File → Last used search files.</summary>
public sealed partial class MainForm
{
    /// <summary>Fills File → Last used search files with the full paths, newest on top ("(empty)" without files).</summary>
    private void FillRecentFilesMenu(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        foreach (string path in _recentFiles.Items)
        {
            ToolStripMenuItem item = new(path);
            item.Click += (_, _) => OpenRecentFile(path);
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
