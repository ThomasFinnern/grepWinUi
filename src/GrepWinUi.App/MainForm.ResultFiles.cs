using GrepWinUi.App.Results;
using GrepWinUi.Core.Search;

namespace GrepWinUi.App;

/// <summary>Main window: File → Save / Open search result.</summary>
public sealed partial class MainForm
{
    /// <summary>
    /// File → Open search result: a result file opens in a new result form that shows the saved
    /// search and its lists (no new search, no search properties dialog).
    /// </summary>
    private void OpenSearchResult()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Open search result",
            Filter = SearchResultFile.DialogFilter,
            InitialDirectory = FileDialogFolder(),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SearchResult result;
        try
        {
            result = SearchResultFile.Load(dialog.FileName);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Search result not loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RememberResultFile(dialog.FileName);
        ShowResult(result);
    }

    /// <summary>
    /// Shows a loaded result: in the result form that already shows the same search (it gets the
    /// query of the result, a running search is stopped), else in a new result form.
    /// </summary>
    private void ShowResult(SearchResult result)
    {
        if (FormWithSameSearch(result.Query) is ResultForm form)
        {
            form.ApplySearch(result.Query.Copy(), form.ViewOptions);
            form.Activate();
        }
        else
        {
            form = CreateResultForm(result.Query.Copy());
        }
        _ = form.ShowResultAsync(result);
    }

    /// <summary>File → Save search result (only when the active result form has a result): result to a file.</summary>
    private void SaveSearchResult()
    {
        if (ActiveMdiChild is not ResultForm { Result: SearchResult result })
        {
            return;
        }

        using SaveFileDialog dialog = new()
        {
            Title = "Save search result",
            Filter = SearchResultFile.DialogFilter,
            AddExtension = false,
            InitialDirectory = FileDialogFolder(),
            FileName = "SearchResult" + SearchResultFile.Extension,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string path = SearchResultFile.WithExtension(dialog.FileName);
        try
        {
            SearchResultFile.Save(result, path);
            RememberResultFile(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Search result not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Remembers the result file as last used (start folder of the file dialogs), puts it on top of the
    /// recent files and saves both.
    /// </summary>
    private void RememberResultFile(string path)
    {
        _lastUsed.RememberResultFile(path);
        SaveLastUsed();
        AddRecentFile(path);
    }
}
