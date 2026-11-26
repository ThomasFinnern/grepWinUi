using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GrepWinUi.App.Results;

/// <summary>
/// Search result window (MDI child): header line with view options, file list pane on top,
/// matching lines pane below. Shows dummy content until searching works.
/// </summary>
internal sealed class ResultForm : Form
{
    private readonly WebView2 _fileListPane = Pane();
    private readonly WebView2 _matchingLinesPane = Pane();
    private readonly Task<CoreWebView2Environment> _webViewEnvironment;
    private readonly ViewOptionsBar _optionsBar = new();
    private readonly int _number;

    /// <summary>Creates a result form.</summary>
    /// <param name="query">Own search query of this form.</param>
    /// <param name="viewOptions">Own copy of the view options (taken from the config).</param>
    /// <param name="webViewEnvironment">Shared WebView2 environment (user data in the user profile).</param>
    /// <param name="number">Running number, shown in front of the title.</param>
    public ResultForm(SearchQuery query, ResultViewOptions viewOptions, Task<CoreWebView2Environment> webViewEnvironment, int number)
    {
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

        Load += async (_, _) => await ShowDummyContentAsync();
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

    private async Task ShowDummyContentAsync()
    {
        try
        {
            CoreWebView2Environment environment = await _webViewEnvironment;
            await _fileListPane.EnsureCoreWebView2Async(environment);
            await _matchingLinesPane.EnsureCoreWebView2Async(environment);
            _fileListPane.NavigateToString(DummyResultHtml.FileList);
            _matchingLinesPane.NavigateToString(DummyResultHtml.MatchingLines);
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            MessageBox.Show(this, "The result view needs the Microsoft Edge WebView2 Runtime.\n\n" + ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
