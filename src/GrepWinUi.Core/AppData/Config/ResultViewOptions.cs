namespace GrepWinUi.Core.AppData.Config;

/// <summary>
/// View options of a result form. The config holds the standard values;
/// each new result form starts with a copy: <c>config.ResultView with { }</c>.
/// </summary>
public sealed record ResultViewOptions
{
    /// <summary>Lines kept (prepared) before a matching line during the search.</summary>
    public int KeepLinesBefore { get; set; } = 0;

    /// <summary>Lines kept (prepared) after a matching line during the search.</summary>
    public int KeepLinesAfter { get; set; } = 0;

    /// <summary>Lines shown before a matching line (at most <see cref="KeepLinesBefore"/>).</summary>
    public int ShowLinesBefore { get; set; } = 0;

    /// <summary>Lines shown after a matching line (at most <see cref="KeepLinesAfter"/>).</summary>
    public int ShowLinesAfter { get; set; } = 0;

    /// <summary>Show the title (header) of the result panes.</summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>Show line numbers in front of matching lines.</summary>
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Use a fixed-width font for matching lines.</summary>
    public bool FixedFont { get; set; } = true;

    /// <summary>Show the file name above its matching lines.</summary>
    public bool ShowFileNames { get; set; } = true;

    /// <summary>Show the matching lines (file contents).</summary>
    public bool ShowFileContents { get; set; } = true;

    /// <summary>Show the whole line, not only the matching part.</summary>
    public bool ShowWholeLine { get; set; } = true;

    /// <summary>Keep double blanks in lines (false: reduce them to one).</summary>
    public bool ShowDoubleBlanks { get; set; } = false;
}
