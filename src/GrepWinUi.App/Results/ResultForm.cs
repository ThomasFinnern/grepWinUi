using GrepWinUi.Core.AppData.Config;
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

    /// <summary>Creates a result form.</summary>
    /// <param name="viewOptions">Own copy of the view options (taken from the config).</param>
    /// <param name="webViewEnvironment">Shared WebView2 environment (user data in the user profile).</param>
    /// <param name="number">Running number, shown in the dummy title.</param>
    public ResultForm(ResultViewOptions viewOptions, Task<CoreWebView2Environment> webViewEnvironment, int number)
    {
        ViewOptions = viewOptions;
        _webViewEnvironment = webViewEnvironment;

        Text = $"[{number}] [public|protected|private]\\s+function :: *controller.php :: e:\\wamp64\\www\\api_6x\\api\\components";
        Size = new Size(900, 600);

        ViewOptionsBar optionsBar = new();
        optionsBar.LoadFrom(ViewOptions);

        SplitContainer panes = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.Fixed3D,
        };
        panes.Panel1.Controls.Add(_fileListPane);
        panes.Panel2.Controls.Add(_matchingLinesPane);

        Controls.Add(panes);
        Controls.Add(optionsBar);

        Load += async (_, _) => await ShowDummyContentAsync();
    }

    /// <summary>View options of this form (independent copy of the config values).</summary>
    public ResultViewOptions ViewOptions { get; }

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
