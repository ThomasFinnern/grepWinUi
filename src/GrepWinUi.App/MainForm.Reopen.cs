using GrepWinUi.App.Results;
using GrepWinUi.Core.AppData.Session;
using GrepWinUi.Core.Search;

namespace GrepWinUi.App;

/// <summary>Main window: result forms open on exit come back on the next start.</summary>
public sealed partial class MainForm
{
    /// <summary>Folder of the result files of the windows to reopen (%AppData%\GrepWinUi\Reopen).</summary>
    private string ReopenFolder => Path.Combine(_store.Folders.UserFolder, ReopenSearches.FolderName);

    /// <summary>
    /// On exit, with Settings "Reopen last open searches" or "Show last search result": writes every result
    /// form (query, view options, result file) for the next start. Problems are ignored: closing never fails.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel && (Config.ReopenLastSearchesOnStart || Config.ShowLastResultOnStart))
        {
            SaveOpenSearches();
        }
    }

    private void SaveOpenSearches()
    {
        List<ResultForm> forms = [.. MdiChildren.OfType<ResultForm>()];
        List<ReopenWindow> windows = [.. forms.Select(form => new ReopenWindow(form.Query, form.Result, form.ViewOptions))];
        int activeIndex = ActiveMdiChild is ResultForm active ? forms.IndexOf(active) : -1;
        try
        {
            _store.Save(ReopenSearches.WriteWindows(ReopenFolder, windows, activeIndex));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Only the windows of the next start are lost.
        }
    }

    /// <summary>
    /// Program start: opens the result forms of the last exit (all with Settings "Reopen last open searches",
    /// only the active one with "Show last search result"), each with its query, view options and saved
    /// result (no new search); the active one is active again. A result file that can't be read is skipped
    /// (the form shows its query without result). Returns the active form (the last opened one when none was
    /// active on exit), null when no form was opened.
    /// </summary>
    private ResultForm? ReopenLastSearches()
    {
        ReopenSearches reopen = _store.Load<ReopenSearches>().Value;
        IReadOnlyList<ReopenEntry> entries = reopen.ToOpen(Config.ReopenLastSearchesOnStart, Config.ShowLastResultOnStart);
        ResultForm? toActivate = null;
        ResultForm? last = null;
        foreach (ReopenEntry entry in entries)
        {
            ResultForm form = CreateResultForm(entry.Query.Copy(), entry.View);
            last = form;
            if (ReadReopenResult(entry.ResultFile) is SearchResult result)
            {
                _ = form.ShowResultAsync(result);
            }
            if (ReferenceEquals(entry, reopen.Active))
            {
                toActivate = form;
            }
        }
        toActivate?.Activate();
        return toActivate ?? last;
    }

    /// <summary>Result of a window to reopen; null without a file or when it can't be read.</summary>
    private static SearchResult? ReadReopenResult(string path)
    {
        if (path.Length == 0)
        {
            return null;
        }
        try
        {
            return SearchResultFile.Load(path);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
