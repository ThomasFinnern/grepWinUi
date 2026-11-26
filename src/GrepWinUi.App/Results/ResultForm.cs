using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using SaveAndLoad;

namespace GrepWinUi.App.Results;

/// <summary>
/// Search result window (MDI child): header line with view options, file list pane on top,
/// matching lines pane below. The pages are built in Core (<see cref="ResultPageHtml"/>) with the
/// pane style sheets (<see cref="ResultCss"/>), read again for every page so CSS edits show on the next search.
/// </summary>
internal sealed class ResultForm : Form
{
    private readonly WebView2 _fileListPane = Pane();
    private readonly WebView2 _matchingLinesPane = Pane();
    private readonly Task<CoreWebView2Environment> _webViewEnvironment;
    private readonly ViewOptionsBar _optionsBar = new();
    private readonly int _number;
    private readonly StorageFolders _cssFolders;
    private Task<bool> _panesReady = Task.FromResult(false);
    private CancellationTokenSource? _search;

    /// <summary>Creates a result form.</summary>
    /// <param name="query">Own search query of this form.</param>
    /// <param name="viewOptions">Own copy of the view options (taken from the config).</param>
    /// <param name="webViewEnvironment">Shared WebView2 environment (user data in the user profile).</param>
    /// <param name="number">Running number, shown in front of the title.</param>
    /// <param name="cssFolders">User and defaults folder of the pane style sheets.</param>
    public ResultForm(SearchQuery query, ResultViewOptions viewOptions, Task<CoreWebView2Environment> webViewEnvironment,
        int number, StorageFolders cssFolders)
    {
        _cssFolders = cssFolders;
        Query = query;
        ViewOptions = viewOptions;
        _webViewEnvironment = webViewEnvironment;
        _number = number;

        UpdateTitle();
        Size = new Size(900, 600);
        _optionsBar.Bind(ViewOptions);

        SplitContainer panes = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.Fixed3D,
        };
        panes.Panel1.Controls.Add(_fileListPane);
        panes.Panel2.Controls.Add(_matchingLinesPane);

        Controls.Add(panes);
        Controls.Add(_optionsBar);

        _fileListPane.KeyDown += OnPaneKeyDown;
        _matchingLinesPane.KeyDown += OnPaneKeyDown;
        Load += (_, _) => _panesReady = InitPanesAsync();
    }

    /// <summary>
    /// Shortcut keys pressed inside a WebView2 pane don't reach the main menu by themselves
    /// (the browser would e.g. open its own find bar on Ctrl+F). A key that belongs to a working
    /// menu item makes this form the active one and runs the item; other keys stay with the pane.
    /// </summary>
    private void OnPaneKeyDown(object? sender, KeyEventArgs e)
    {
        if (MdiParent?.MainMenuStrip is not MenuStrip menu || MainMenu.FindShortcut(menu, e.KeyData) is not ToolStripMenuItem item)
        {
            return;
        }

        e.Handled = true;
        // Run after the WebView2 key event has returned: the item may open a modal dialog.
        BeginInvoke(() =>
        {
            Activate();
            item.PerformClick();
        });
    }

    /// <summary>Search query of this form.</summary>
    public SearchQuery Query { get; private set; }

    /// <summary>View options of this form (independent copy of the config values); the header line edits them.</summary>
    public ResultViewOptions ViewOptions { get; private set; }

    /// <summary>Takes over the edited query and view options (from the search properties dialog).</summary>
    public void ApplySearch(SearchQuery query, ResultViewOptions viewOptions)
    {
        Query = query;
        ViewOptions = viewOptions;
        _optionsBar.Bind(ViewOptions);
        UpdateTitle();
    }

    private void UpdateTitle() => Text = $"[{_number}] {SearchQueryText.Title(Query)}";

    /// <summary>Result pane; background like the form (no white flash before the page is shown).</summary>
    private static WebView2 Pane() => new() { Dock = DockStyle.Fill, DefaultBackgroundColor = SystemColors.Control };

    /// <summary>
    /// Runs the search of <see cref="Query"/> in the background and shows the result in both panes.
    /// A running search of this form is cancelled first. Never throws (problems are shown in the panes).
    /// </summary>
    public async Task RunSearchAsync()
    {
        _search?.Cancel();
        using CancellationTokenSource search = new();
        _search = search;
        SetResult(null);
        await ShowPagesAsync(
            HintPage(ResultCss.FileListFileName, $"Searching '{Query.SearchString}' in '{Query.FolderName}' …"),
            HintPage(ResultCss.MatchingLinesFileName, ""));

        string fileList;
        string matchingLines;
        SearchResult? result = null;
        try
        {
            result = await SearchEngine.RunAsync(Query, progress: null, search.Token);
            fileList = FileListPage(result);
            matchingLines = MatchingLinesPage(result);
        }
        catch (Exception ex)
        {
            // Unexpected error in the background search: show it instead of losing it.
            fileList = HintPage(ResultCss.FileListFileName, "Search failed: " + ex.Message);
            matchingLines = HintPage(ResultCss.MatchingLinesFileName, "");
        }

        if (_search == search)
        {
            _search = null;
            SetResult(result);
            await ShowPagesAsync(fileList, matchingLines);
        }
    }

    /// <summary>True while a search of this form runs.</summary>
    public bool IsSearching => _search is not null;

    /// <summary>
    /// Search → Cancel Search: stops the running search of this form; the panes then show what was
    /// found until then, marked as cancelled. Does nothing when no search runs.
    /// </summary>
    public void CancelSearch() => _search?.Cancel();

    /// <summary>Last finished (or loaded) search result of this form; null before the first one or while searching.</summary>
    public SearchResult? Result { get; private set; }

    /// <summary>
    /// Raised when <see cref="Result"/> or <see cref="IsSearching"/> changes
    /// (e.g. to enable File → Save search result and Search → Cancel Search).
    /// </summary>
    public event EventHandler? ResultChanged;

    /// <summary>Shows a saved result (File → Open search result) without searching; a running search is cancelled.</summary>
    public async Task ShowResultAsync(SearchResult result)
    {
        _search?.Cancel();
        _search = null;
        SetResult(result);
        await ShowPagesAsync(FileListPage(result), MatchingLinesPage(result));
    }

    private void SetResult(SearchResult? result)
    {
        Result = result;
        ResultChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Stops a running search of this form when it is closed.</summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _search?.Cancel();
        base.OnFormClosed(e);
    }

    /// <summary>Form background color as HTML color (e.g. #F0F0F0), so the panes look like the window.</summary>
    private static string Background
    {
        get
        {
            Color color = SystemColors.Control;
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    private string FileListPage(SearchResult result) =>
        ResultPageHtml.FileList(result, Css(ResultCss.FileListFileName), Background);

    private string MatchingLinesPage(SearchResult result) =>
        ResultPageHtml.MatchingLines(result, Css(ResultCss.MatchingLinesFileName), Background);

    private string HintPage(string cssFileName, string text) => ResultPageHtml.Hint(text, Css(cssFileName), Background);

    /// <summary>CSS text of a pane (user file → defaults file → built-in; a file that can't be read is skipped).</summary>
    private string Css(string fileName) => ResultCss.Load(_cssFolders, fileName).Text;

    /// <summary>Starts both WebView2 panes and shows a hint until the first search.</summary>
    private async Task<bool> InitPanesAsync()
    {
        try
        {
            CoreWebView2Environment environment = await _webViewEnvironment;
            await _fileListPane.EnsureCoreWebView2Async(environment);
            await _matchingLinesPane.EnsureCoreWebView2Async(environment);
            _fileListPane.NavigateToString(HintPage(ResultCss.FileListFileName, "No search yet: Search → Search (Ctrl+F), then Start search."));
            _matchingLinesPane.NavigateToString(HintPage(ResultCss.MatchingLinesFileName, ""));
            return true;
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            MessageBox.Show(this, "The result view needs the Microsoft Edge WebView2 Runtime.\n\n" + ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    /// <summary>Shows both pages once the panes are ready (nothing when WebView2 is missing or the form is closed).</summary>
    private async Task ShowPagesAsync(string fileList, string matchingLines)
    {
        if (!await _panesReady || IsDisposed)
        {
            return;
        }
        _fileListPane.NavigateToString(fileList);
        _matchingLinesPane.NavigateToString(matchingLines);
    }
}
