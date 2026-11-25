using SaveAndLoad;

namespace GrepWinUi.Core.AppData.Session;

/// <summary>
/// Files last opened or saved, kept across program starts. Used to open them again
/// on startup or from a menu, and as start folder of open/save dialogs.
/// Saved as LastUsed.json whenever one of them changes.
/// </summary>
public sealed class LastUsedFiles : IStorable
{
    /// <inheritdoc/>
    public static string FileName => "LastUsed.json";

    /// <summary>Last opened or saved search query file.</summary>
    public string SearchQueryFile { get; set; } = "";

    /// <summary>Last opened or saved search result file.</summary>
    public string ResultFile { get; set; } = "";

    /// <summary>Last saved search result as HTML.</summary>
    public string ResultHtmlFile { get; set; } = "";

    /// <summary>The newest of all files above (any kind).</summary>
    public string AnyFile { get; set; } = "";

    /// <summary>Remembers a search query file (also as <see cref="AnyFile"/>).</summary>
    public void RememberSearchQueryFile(string path)
    {
        SearchQueryFile = path;
        AnyFile = path;
    }

    /// <summary>Remembers a result file (also as <see cref="AnyFile"/>).</summary>
    public void RememberResultFile(string path)
    {
        ResultFile = path;
        AnyFile = path;
    }

    /// <summary>Remembers an HTML result file (also as <see cref="AnyFile"/>).</summary>
    public void RememberResultHtmlFile(string path)
    {
        ResultHtmlFile = path;
        AnyFile = path;
    }

    /// <summary>
    /// The given file if set, else <see cref="AnyFile"/>, as in the old program.
    /// Example: <c>OrAnyFile(last.ResultFile)</c> for the result open dialog.
    /// </summary>
    public string OrAnyFile(string file) => file.Length > 0 ? file : AnyFile;
}
