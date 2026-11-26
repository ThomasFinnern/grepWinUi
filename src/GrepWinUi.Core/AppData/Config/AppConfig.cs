using System.Text.Json;
using SaveAndLoad;

namespace GrepWinUi.Core.AppData.Config;

/// <summary>Program settings of the user. Saved as Config.json.</summary>
public sealed class AppConfig : IStorable
{
    /// <inheritdoc/>
    public static string FileName => "Config.json";

    // --- On program start ---

    /// <summary>Open an empty search form on start.</summary>
    public bool OpenSearchFormOnStart { get; set; } = false;

    /// <summary>Run the last search again on start.</summary>
    public bool RepeatLastSearchOnStart { get; set; } = false;

    /// <summary>Reopen the search forms that were open on exit.</summary>
    public bool ReopenLastSearchesOnStart { get; set; } = false;

    /// <summary>Show the last saved search result on start (no new search).</summary>
    public bool ShowLastResultOnStart { get; set; } = false;

    // --- While searching ---

    /// <summary>Show the current file name in the status bar while searching.</summary>
    public bool ShowFileNamesWhileSearching { get; set; } = true;

    /// <summary>Count all files first and show progress in %; otherwise a running animation.</summary>
    public bool ShowProgressInPercent { get; set; } = false;

    // --- Result view ---

    /// <summary>Standard view options; each new result form starts with a copy.</summary>
    public ResultViewOptions ResultView { get; set; } = new();

    // --- Token lists ---

    /// <summary>Limits and dropdown order of the search string token list.</summary>
    public TokenListOptions SearchStringTokens { get; set; } = new();

    /// <summary>Independent deep copy (e.g. for editing in a dialog).</summary>
    public AppConfig Copy() =>
        JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(this))!;
}
