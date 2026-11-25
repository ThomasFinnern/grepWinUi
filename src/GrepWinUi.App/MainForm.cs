using GrepWinUi.Core;

namespace GrepWinUi.App;

/// <summary>Main application window. Will become the MDI container for search forms.</summary>
public sealed class MainForm : Form
{
    /// <summary>Creates the main window with its menu bar.</summary>
    public MainForm()
    {
        Version version = typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0, 0);
        Text = AppInfo.MainWindowTitle(version);
        Size = new Size(1000, 750);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;

        MenuStrip menuStrip = MainMenu.Create();
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);
    }
}
