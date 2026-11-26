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
    public string LastUsedFile { get; set; } = "";

    /// <summary>Remembers a search query file (also as <see cref="LastUsedFile"/>).</summary>
    public void RememberSearchQueryFile(string path)
    {
        SearchQueryFile = path;
        LastUsedFile = path;
    }

    /// <summary>Remembers a result file (also as <see cref="LastUsedFile"/>).</summary>
    public void RememberResultFile(string path)
    {
        ResultFile = path;
        LastUsedFile = path;
    }

    /// <summary>Remembers an HTML result file (also as <see cref="LastUsedFile"/>).</summary>
    public void RememberResultHtmlFile(string path)
    {
        ResultHtmlFile = path;
        LastUsedFile = path;
    }

    /// <summary>
    /// The given file if set, else <see cref="LastUsedFile"/>, as in the old program.
    /// Example: <c>OrLastUsedFile(last.ResultFile)</c> for the result open dialog.
    /// </summary>
    public string OrLastUsedFile(string file) => file.Length > 0 ? file : LastUsedFile;

    /// <summary>
    /// Start folder of all file dialogs (search queries now, search results later): the folder of the
    /// last saved / opened / loaded file (<see cref="LastUsedFile"/>) if it exists, else <paramref name="appFolder"/>.
    /// </summary>
    public string FileDialogFolder(string appFolder) => StartFolder(LastUsedFile, appFolder);

    /// <summary>
    /// Start folder of a file dialog: the folder of <paramref name="lastFile"/> if it exists,
    /// else <paramref name="fallbackFolder"/>.
    /// </summary>
    public static string StartFolder(string lastFile, string fallbackFolder)
    {
        if (lastFile.Length == 0)
        {
            return fallbackFolder;
        }

        try
        {
            string? folder = Path.GetDirectoryName(lastFile);
            return !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? folder : fallbackFolder;
        }
        catch (ArgumentException)
        {
            return fallbackFolder;
        }
    }
}
