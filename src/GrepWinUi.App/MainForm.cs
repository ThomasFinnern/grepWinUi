using GrepWinUi.App.Options;
using GrepWinUi.Core;
using GrepWinUi.Core.AppData.Config;
using SaveAndLoad;

namespace GrepWinUi.App;

/// <summary>Main application window. Will become the MDI container for search forms.</summary>
public sealed class MainForm : Form
{
    private readonly ObjectStore _store;

    /// <summary>Creates the main window with its menu bar and status bar.</summary>
    /// <param name="config">Loaded user settings.</param>
    /// <param name="store">Store used to save the settings.</param>
    public MainForm(AppConfig config, ObjectStore store)
    {
        Config = config;
        _store = store;

        Version version = typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0, 0);
        Text = AppInfo.MainWindowTitle(version);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Size = new Size(1000, 750);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;

        MenuStrip menuStrip = MainMenu.Create(new MainMenuActions(OpenSettings, ExitApp));
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);
        Controls.Add(new MainStatusBar());
    }

    /// <summary>User settings; new result forms start with a copy of <see cref="AppConfig.ResultView"/>.</summary>
    internal AppConfig Config { get; private set; }

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
