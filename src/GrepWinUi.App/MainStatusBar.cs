using GrepWinUi.Core.Search;

namespace GrepWinUi.App;

/// <summary>
/// Status bar of the main window: Title, Info, Working (progress), Folder, Files, Time
/// (see "UI spec: status bar" in CLAUDE.md). Shows the <see cref="SearchStatus"/> of the active result form.
/// </summary>
internal sealed class MainStatusBar : StatusStrip
{
    private readonly ToolStripStatusLabel _title = Field();
    private readonly ToolStripStatusLabel _info = Field();
    private readonly ToolStripProgressBar _working = new()
    {
        Minimum = 0,
        Maximum = 100,
        Size = new Size(100, 16),
        MarqueeAnimationSpeed = 30,
    };
    private readonly ToolStripStatusLabel _folder = Field();
    private readonly ToolStripStatusLabel _files = Field();
    private readonly ToolStripStatusLabel _time = Field();

    /// <summary>Creates the status bar with empty fields.</summary>
    public MainStatusBar()
    {
        _info.Spring = true;
        _info.TextAlign = ContentAlignment.MiddleLeft;
        Items.AddRange(new ToolStripItem[] { _title, _info, Field("Working:"), _working, _folder, _files, _time });
        ShowStatus(SearchStatus.Idle);
    }

    /// <summary>
    /// Shows <paramref name="status"/>. Working while the search runs: the percent when all files were
    /// counted first, else a running animation (marquee).
    /// </summary>
    public void ShowStatus(SearchStatus status)
    {
        _title.Text = "Title: " + status.Title;
        _info.Text = "Info: " + status.Info;
        _folder.Text = "Folder: " + status.Folder;
        _files.Text = "Files: " + status.Files;
        _time.Text = "Time: " + status.Time;

        bool marquee = status.IsRunning && status.Percent is null;
        ProgressBarStyle style = marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        if (_working.Style != style)
        {
            _working.Style = style;
        }
        _working.Value = status.IsRunning ? status.Percent ?? 0 : 0;
    }

    /// <summary>Status bar text field with a separator line on its left side.</summary>
    private static ToolStripStatusLabel Field(string text = "") => new(text)
    {
        BorderSides = ToolStripStatusLabelBorderSides.Left,
        BorderStyle = Border3DStyle.Etched,
    };
}
