namespace GrepWinUi.Core.AppData.Config;

/// <summary>
/// Limits and dropdown order of a token list (used items newest first + fixed items), as in the old program.
/// Dropdown: <see cref="PreFixedCount"/> newest used items, then up to <see cref="FirstFixedCount"/>
/// fixed items, then the other used items, then the other fixed items.
/// </summary>
public sealed record TokenListOptions
{
    /// <summary>Number of newest used items shown before the fixed items.</summary>
    public int PreFixedCount { get; set; } = 7;

    /// <summary>Number of fixed items shown right after them.</summary>
    public int FirstFixedCount { get; set; } = 3;

    /// <summary>Maximum number of kept used items (at least 1).</summary>
    public int MaxCount { get; set; } = 30;

    /// <summary>Maximum number of kept fixed items.</summary>
    public int MaxFixedCount { get; set; } = 15;
}
