namespace GrepWinUi.App;

/// <summary>
/// Status bar of the main window: Title, Info, Working (progress), Folder, Files, Time.
/// For now it shows dummy texts only (see "UI spec: status bar" in CLAUDE.md).
/// </summary>
internal sealed class MainStatusBar : StatusStrip
{
    /// <summary>Creates the status bar with dummy texts in all fields.</summary>
    public MainStatusBar()
    {
        ToolStripStatusLabel title = Field("Title: Search 'function'");
        ToolStripStatusLabel info = Field("Info: 154 matches in 48 files from 57 files searched");
        info.Spring = true;
        info.TextAlign = ContentAlignment.MiddleLeft;

        ToolStripStatusLabel working = Field("Working:");
        ToolStripProgressBar progress = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 40,
            Size = new Size(100, 16),
        };

        ToolStripStatusLabel folder = Field("Folder: api_6x");
        ToolStripStatusLabel files = Field("Files: 23 / 57");
        ToolStripStatusLabel time = Field("Time: 00:00:07");

        Items.AddRange(new ToolStripItem[] { title, info, working, progress, folder, files, time });
    }

    /// <summary>Status bar text field with a separator line on its left side.</summary>
    private static ToolStripStatusLabel Field(string text) => new(text)
    {
        BorderSides = ToolStripStatusLabelBorderSides.Left,
        BorderStyle = Border3DStyle.Etched,
    };
}
