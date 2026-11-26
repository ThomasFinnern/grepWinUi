using GrepWinUi.App.Options;
using GrepWinUi.App.Results;
using GrepWinUi.App.Search;
using GrepWinUi.Core;
using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using Microsoft.Web.WebView2.Core;
using SaveAndLoad;

namespace GrepWinUi.App;

/// <summary>Main application window: MDI container for the search result forms.</summary>
public sealed class MainForm : Form
{
    private readonly ObjectStore _store;
    private readonly SearchLists _lists;
    private Task<CoreWebView2Environment>? _webViewEnvironment;
    private int _resultFormCount;

    /// <summary>Creates the main window with its menu bar and status bar.</summary>
    /// <param name="config">Loaded user settings.</param>
    /// <param name="lists">Loaded recent lists of the search form.</param>
    /// <param name="store">Store used to save the settings and lists.</param>
    public MainForm(AppConfig config, SearchLists lists, ObjectStore store)
    {
        Config = config;
        _lists = lists;
        _store = store;

        Version version = typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0, 0);
        Text = AppInfo.MainWindowTitle(version);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Size = new Size(1000, 750);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;

        MenuStrip menuStrip = MainMenu.Create(new MainMenuActions(OpenSettings, ExitApp, NewSearch, EditSearch, LayoutMdi));
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);
        Controls.Add(new MainStatusBar());

        Shown += (_, _) => OnFirstShown();
    }

    /// <summary>Program start: without an open result form, a new empty one with its search properties dialog.</summary>
    private void OnFirstShown()
    {
        if (MdiChildren.Length == 0)
        {
            NewSearch();
        }
    }

    /// <summary>User settings; new result forms start with a copy of <see cref="AppConfig.ResultView"/>.</summary>
    internal AppConfig Config { get; private set; }

    /// <summary>
    /// File → New search query: opens a new result form with the newest past query (or the defaults)
    /// and a copy of the view settings, then its search properties dialog.
    /// </summary>
    private void NewSearch()
    {
        _resultFormCount++;
        ResultForm form = new(_lists.StartQuery(Config), Config.ResultView with { }, WebViewEnvironment(), _resultFormCount)
        {
            MdiParent = this,
            Icon = Icon,
        };
        form.Show();
        ShowSearchProperties(form);
    }

    /// <summary>Search → Search: search properties of the active result form; without one a new result form.</summary>
    private void EditSearch()
    {
        if (ActiveMdiChild is ResultForm form)
        {
            ShowSearchProperties(form);
        }
        else
        {
            NewSearch();
        }
    }

    /// <summary>
    /// Search properties dialog, modal over <paramref name="form"/>, filled with its query and line counts.
    /// Exit / Start search: the form takes over the edited values, the lists remember them and are saved
    /// (Start search also remembers the whole query). Cancel changes nothing.
    /// </summary>
    private void ShowSearchProperties(ResultForm form)
    {
        using SearchPropertiesForm dialog = new(form.Query, form.ViewOptions, _lists);
        if (dialog.ShowDialog(form) != DialogResult.OK)
        {
            return;
        }

        form.ApplySearch(dialog.Query, dialog.ViewOptions);
        _lists.Remember(dialog.Query, asPastQuery: dialog.StartSearchRequested);
        try
        {
            _lists.Save(_store);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Search lists not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Shared WebView2 environment, created on first use. Its cache lives in
    /// %LocalAppData%\GrepWinUi\WebView2 (default would be next to the exe: needs admin rights).
    /// </summary>
    private Task<CoreWebView2Environment> WebViewEnvironment() =>
        _webViewEnvironment ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppInfo.ProductName,
                "WebView2"));

    /// <summary>File → Exit: closes the main window and ends the program.</summary>
    private void ExitApp() => Close();

    /// <summary>Options → Settings: edit and save the settings.</summary>
    private void OpenSettings()
    {
        using SettingsForm dialog = new(Config);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
        {
            return;
        }

        try
        {
            _store.Save(dialog.Result);
            Config = dialog.Result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Settings not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
