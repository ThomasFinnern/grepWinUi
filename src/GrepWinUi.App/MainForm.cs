using GrepWinUi.App.Options;
using GrepWinUi.App.Results;
using GrepWinUi.App.Search;
using GrepWinUi.Core;
using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.AppData.Session;
using Microsoft.Web.WebView2.Core;
using RecentLists;
using SaveAndLoad;

namespace GrepWinUi.App;

/// <summary>Main application window: MDI container for the search result forms.</summary>
public sealed class MainForm : Form
{
    private readonly ObjectStore _store;
    private readonly SearchLists _lists;
    private readonly LastUsedFiles _lastUsed;
    private readonly ToolStripItem _saveQueryItem;
    private Task<CoreWebView2Environment>? _webViewEnvironment;
    private int _resultFormCount;

    /// <summary>Creates the main window with its menu bar and status bar.</summary>
    /// <param name="config">Loaded user settings.</param>
    /// <param name="lists">Loaded recent lists of the search form.</param>
    /// <param name="lastUsed">Last used files (start folders of the file dialogs).</param>
    /// <param name="store">Store used to save the settings, lists and last used files.</param>
    public MainForm(AppConfig config, SearchLists lists, LastUsedFiles lastUsed, ObjectStore store)
    {
        Config = config;
        _lists = lists;
        _lastUsed = lastUsed;
        _store = store;

        Version version = typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0, 0);
        Text = AppInfo.MainWindowTitle(version);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Size = new Size(1000, 750);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;

        MenuStrip menuStrip = MainMenu.Create(new MainMenuActions(
            OpenSettings, ExitApp, NewSearch, EditSearch, OpenSearchQuery, LoadSearchQuery, SaveSearchQuery,
            EditSearchStringTokens, EditFileNameTokens, LayoutMdi));
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);
        Controls.Add(new MainStatusBar());

        _saveQueryItem = menuStrip.Items.Find(MainMenu.SaveSearchQueryName, searchAllChildren: true).Single();
        UpdateMenuState();
        MdiChildActivate += (_, _) => UpdateMenuState();
        Shown += (_, _) => OnFirstShown();
    }

    /// <summary>Items that need an active result form are greyed out without one.</summary>
    private void UpdateMenuState() => _saveQueryItem.Enabled = ActiveMdiChild is ResultForm;

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
    private void NewSearch() => OpenResultForm(_lists.StartQuery(Config));

    /// <summary>New result form with <paramref name="query"/> and a copy of the view settings, then its search properties dialog.</summary>
    private void OpenResultForm(SearchQuery query)
    {
        _resultFormCount++;
        ResultForm form = new(query, Config.ResultView with { }, WebViewEnvironment(), _resultFormCount)
        {
            MdiParent = this,
            Icon = Icon,
        };
        form.FormClosed += (_, _) => BeginInvoke(UpdateMenuState);
        form.Show();
        ShowSearchProperties(form);
    }

    /// <summary>File → Open search query: a query file opens in a new result form with its search properties dialog.</summary>
    private void OpenSearchQuery()
    {
        if (ReadQueryFile("Open search query") is SearchQuery query)
        {
            OpenResultForm(query);
        }
    }

    /// <summary>
    /// File → Load search query: a query file replaces the query of the active result form
    /// (its line counts stay), then its search properties dialog. Without an active form: like Open.
    /// </summary>
    private void LoadSearchQuery()
    {
        if (ReadQueryFile("Load search query") is not SearchQuery query)
        {
            return;
        }

        if (ActiveMdiChild is ResultForm form)
        {
            form.ApplySearch(query, form.ViewOptions);
            ShowSearchProperties(form);
        }
        else
        {
            OpenResultForm(query);
        }
    }

    /// <summary>File → Save search query (only with an active result form): its query to a file.</summary>
    private void SaveSearchQuery()
    {
        if (ActiveMdiChild is not ResultForm form)
        {
            return;
        }

        using SaveFileDialog dialog = new()
        {
            Title = "Save search query",
            Filter = SearchQueryFile.DialogFilter,
            DefaultExt = SearchQueryFile.Extension,
            InitialDirectory = QueryFileFolder(),
            FileName = "SearchQuery" + SearchQueryFile.Extension,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SearchQueryFile.Save(form.Query, dialog.FileName);
            RememberQueryFile(dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Search query not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Asks for a query file and reads it; null when cancelled or the file can't be read (warning shown).</summary>
    private SearchQuery? ReadQueryFile(string title)
    {
        using OpenFileDialog dialog = new()
        {
            Title = title,
            Filter = SearchQueryFile.DialogFilter,
            InitialDirectory = QueryFileFolder(),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return null;
        }

        try
        {
            SearchQuery query = SearchQueryFile.Load(dialog.FileName);
            RememberQueryFile(dialog.FileName);
            return query;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Search query not loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
    }

    /// <summary>
    /// Start folder of the file dialogs: folder of the last saved / opened / loaded file,
    /// at the beginning the app's user folder (%AppData%\GrepWinUi, created if missing).
    /// </summary>
    private string QueryFileFolder()
    {
        string appFolder = _store.Folders.UserFolder;
        try
        {
            Directory.CreateDirectory(appFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The dialog then starts in its own default folder.
        }
        return _lastUsed.FileDialogFolder(appFolder);
    }

    /// <summary>Remembers the query file as last used and saves <c>LastUsed.json</c> (problems are ignored: only a start folder).</summary>
    private void RememberQueryFile(string path)
    {
        _lastUsed.RememberSearchQueryFile(path);
        try
        {
            _store.Save(_lastUsed);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Only the start folder of the next file dialog is lost.
        }
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
        using SearchPropertiesForm dialog = new(form.Query, form.ViewOptions, _lists, Config);
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
    /// <summary>Options → Search string token: edit the search string list and its settings.</summary>
    private void EditSearchStringTokens() => EditTokenList(
        "Search string token", _lists.SearchStrings, StringComparer.Ordinal,
        config => config.SearchStringTokens, (config, options) => config.SearchStringTokens = options);

    /// <summary>Options → File name token: edit the file names list and its settings.</summary>
    private void EditFileNameTokens() => EditTokenList(
        "File name token", _lists.FileNames, StringComparer.OrdinalIgnoreCase,
        config => config.FileNameTokens, (config, options) => config.FileNameTokens = options);

    /// <summary>
    /// Token list dialog: edit a list (fixed entries ticked) and its four list settings.
    /// Exit stores the settings in <c>Config.json</c> (same values as in Settings), replaces the list
    /// and saves the lists; Cancel changes nothing.
    /// </summary>
    private void EditTokenList(
        string title,
        PinnedRecentList<string> list,
        StringComparer comparer,
        Func<AppConfig, TokenListOptions> getOptions,
        Action<AppConfig, TokenListOptions> setOptions)
    {
        TokenListOptions options = getOptions(Config);
        using TokenListForm dialog = new(
            title,
            list.EditRows(options.PreFixedCount, options.FirstFixedCount),
            comparer,
            options);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Rows is null || dialog.Options is null)
        {
            return;
        }

        AppConfig changed = Config.Copy();
        setOptions(changed, dialog.Options);
        try
        {
            _store.Save(changed);
            Config = changed;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Settings not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        _lists.ApplySettings(Config);
        list.ReplaceAll(dialog.Rows);
        try
        {
            _lists.Save(_store);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Search lists not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

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
            _lists.ApplySettings(Config);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Settings not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
