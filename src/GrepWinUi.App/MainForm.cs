using GrepWinUi.Core;
using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.App;

/// <summary>Main application window. Will become the MDI container for search forms.</summary>
public sealed class MainForm : Form
{
    /// <summary>Creates the main window with its menu bar and status bar.</summary>
    /// <param name="config">Loaded user settings.</param>
    public MainForm(AppConfig config)
    {
        Config = config;

        Version version = typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0, 0);
        Text = AppInfo.MainWindowTitle(version);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Size = new Size(1000, 750);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;

        MenuStrip menuStrip = MainMenu.Create();
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);
        Controls.Add(new MainStatusBar());
    }

    /// <summary>User settings; new result forms start with a copy of <see cref="AppConfig.ResultView"/>.</summary>
    internal AppConfig Config { get; }
}
