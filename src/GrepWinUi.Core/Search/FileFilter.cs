using System.IO.Enumeration;
using System.Text.RegularExpressions;
using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// File and folder filters of a search query: file names (wildcards or RegEx), skip by extension,
/// skip folders, folder names as RegEx (on the path below the start folder), base name as RegEx.
/// All comparisons ignore upper / lower case; a RegEx only needs to match a part of the text.
/// </summary>
public sealed class FileFilter
{
    private readonly IReadOnlyList<string> _filePatterns;
    private readonly Regex? _fileRegex;
    private readonly IReadOnlyList<string> _skippedFilePatterns;
    private readonly IReadOnlyList<string> _skippedFolders;
    private readonly Regex? _folderRegex;
    private readonly RegexTarget _folderRegexTarget;
    private readonly Regex? _baseNameRegex;

    private FileFilter(SearchQuery query)
    {
        _fileRegex = query.FileNamesAsRegex ? CreateRegex("File names", query.FileNames) : null;
        _filePatterns = FileFinder.Patterns(query.FileNames);
        _skippedFilePatterns = query.SkipByExtension ? ExtensionPatterns(query.SkippedExtensions) : [];
        _skippedFolders = query.SkipFolders ? Words(query.SkippedFolders) : [];
        _folderRegex = query.FolderNamesAsRegex ? CreateRegex("Folder names", query.FolderNamePattern) : null;
        _folderRegexTarget = query.RegularExpressionOn;
        _baseNameRegex = query.BaseNameAsRegex ? CreateRegex("Base name", query.BaseNamePattern) : null;
    }

    /// <summary>Filter for the options of <paramref name="query"/>.</summary>
    /// <exception cref="ArgumentException">A RegEx is not valid; the message starts with the input name.</exception>
    public static FileFilter Create(SearchQuery query) => new(query);

    /// <summary>
    /// Problems of the RegEx inputs that are switched on (search string, file names, folder names,
    /// base name), e.g. for the search dialog; empty when all are valid.
    /// </summary>
    public static IReadOnlyList<string> RegexProblems(SearchQuery query)
    {
        List<string> problems = [];
        void Check(bool on, string name, string pattern)
        {
            if (on && pattern.Length > 0 && LineMatcher.RegexError(pattern) is string error)
            {
                problems.Add($"{name}: {error}");
            }
        }
        Check(query.RegularExpression, "Search string", query.SearchString);
        Check(query.FileNamesAsRegex, "File names", query.FileNames);
        Check(query.FolderNamesAsRegex, "Folder names", query.FolderNamePattern);
        Check(query.BaseNameAsRegex, "Base name", query.BaseNamePattern);
        return problems;
    }

    /// <summary>
    /// Problems that stop "Start search" in the search dialog: invalid RegEx inputs
    /// (<see cref="RegexProblems"/>), "Skip binary files" together with "Only binary files", and
    /// "Modified between" with the start day after the end day.
    /// </summary>
    public static IReadOnlyList<string> StartProblems(SearchQuery query)
    {
        List<string> problems = [.. RegexProblems(query)];
        if (query.SkipBinaryFiles && query.OnlyBinaryFiles)
        {
            problems.Add("\"Skip binary files\" and \"Only binary files\" together search no file.");
        }
        if (FileDateFilter.Problem(query) is string dateProblem)
        {
            problems.Add(dateProblem);
        }
        return problems;
    }

    /// <summary>
    /// True when the files of a folder are searched: base name as RegEx matches the folder's last name
    /// (always true without that option).
    /// </summary>
    public bool SearchesFilesIn(string folder) =>
        _baseNameRegex is null || _baseNameRegex.IsMatch(BaseName(folder));

    /// <summary>True when a subfolder is skipped ("Skip folders": its last name matches a skip entry).</summary>
    public bool SkipsFolder(string subfolder)
    {
        string name = BaseName(subfolder);
        return _skippedFolders.Any(pattern => Wildcard(pattern, name));
    }

    /// <summary>
    /// True when a file is searched: its name matches the file names, its extension is not skipped and
    /// folder names as RegEx matches the path below the start folder.
    /// </summary>
    /// <param name="relativeFolder">Folder of the file below the start folder ("" for the start folder itself).</param>
    /// <param name="fileName">File name without folder.</param>
    public bool Accepts(string relativeFolder, string fileName)
    {
        bool nameMatches = _fileRegex?.IsMatch(fileName) ?? FileFinder.Matches(fileName, _filePatterns);
        if (!nameMatches || _skippedFilePatterns.Any(pattern => Wildcard(pattern, fileName)))
        {
            return false;
        }
        if (_folderRegex is null)
        {
            return true;
        }
        string target = _folderRegexTarget == RegexTarget.PathName ? relativeFolder : Path.Join(relativeFolder, fileName);
        return _folderRegex.IsMatch(target);
    }

    /// <summary>
    /// Skip by extension entries as file name wildcards: "*.exe" stays, ".exe" and "exe" become "*.exe".
    /// </summary>
    public static IReadOnlyList<string> ExtensionPatterns(string text) =>
        [.. Words(text).Select(word => word.Contains('*') || word.Contains('?') ? word : "*." + word.TrimStart('.'))];

    /// <summary>Last name of a folder path (also for a path with a trailing backslash).</summary>
    public static string BaseName(string folder) => Path.GetFileName(folder.TrimEnd('\\', '/'));

    private static IReadOnlyList<string> Words(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static bool Wildcard(string pattern, string name) =>
        FileSystemName.MatchesWin32Expression(FileSystemName.TranslateWin32Expression(pattern), name, ignoreCase: true);

    private static Regex CreateRegex(string inputName, string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, LineMatcher.RegexTimeout);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"{inputName}: {ex.Message}", ex);
        }
    }
}
