using System.Text.Json;
using System.Text.Json.Serialization;
using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.Core.AppData.Search;

/// <summary>
/// All inputs of the search properties form: search string, file names, folder name and their options.
/// Line counts before/after a match are not here; they belong to the result form's view options.
/// </summary>
public sealed class SearchQuery
{
    // --- Search string ---

    /// <summary>Text or regular expression to search for.</summary>
    public string SearchString { get; set; } = "";

    /// <summary>Match whole words only.</summary>
    public bool WholeWordsOnly { get; set; } = false;

    /// <summary>Upper and lower case must match.</summary>
    public bool MatchCase { get; set; } = false;

    /// <summary>The search string is a regular expression.</summary>
    public bool RegularExpression { get; set; } = false;

    /// <summary>Stop reading a file after its first match.</summary>
    public bool FirstMatchPerFileOnly { get; set; } = false;

    /// <summary>List the files that have no match.</summary>
    public bool ListFilesWithoutMatch { get; set; } = false;

    /// <summary>Show the lines that do not match.</summary>
    public bool NonMatchingLines { get; set; } = false;

    /// <summary>Search only in the files of the current result.</summary>
    public bool OnlyInFilesOfCurrentResult { get; set; } = false;

    /// <summary>The search string must match the whole line.</summary>
    public bool MatchWholeLine { get; set; } = false;

    /// <summary>Show file names only, no lines.</summary>
    public bool FileNamesOnly { get; set; } = false;

    /// <summary>Search only in a fixed-width field of each line.</summary>
    public bool FixedWidthField { get; set; } = false;

    /// <summary>First column (1-based) of the fixed-width field.</summary>
    public int FixedWidthStartColumn { get; set; } = 1;

    /// <summary>Width of the fixed-width field in characters.</summary>
    public int FixedWidthWidth { get; set; } = 0;

    /// <summary>Search only in one field of delimited lines.</summary>
    public bool DelimitedField { get; set; } = false;

    /// <summary>Separator character of the delimited fields.</summary>
    public string DelimitedSeparator { get; set; } = ",";

    /// <summary>Number (1-based) of the delimited field to search in.</summary>
    public int DelimitedFieldNumber { get; set; } = 1;

    /// <summary>Filter files by their modification date.</summary>
    public bool FilterByFileDate { get; set; } = false;

    /// <summary>Only files modified within the last <see cref="ModifiedWithinValue"/> units.</summary>
    public bool ModifiedWithinLast { get; set; } = false;

    /// <summary>Count for <see cref="ModifiedWithinLast"/>.</summary>
    public int ModifiedWithinValue { get; set; } = 1;

    /// <summary>Unit for <see cref="ModifiedWithinLast"/>.</summary>
    public TimeUnit ModifiedWithinUnit { get; set; } = TimeUnit.Days;

    /// <summary>Only files modified between <see cref="ModifiedFrom"/> and <see cref="ModifiedTo"/>.</summary>
    public bool ModifiedBetween { get; set; } = false;

    /// <summary>Start date of <see cref="ModifiedBetween"/>; null = no start.</summary>
    public DateTime? ModifiedFrom { get; set; } = null;

    /// <summary>End date of <see cref="ModifiedBetween"/>; null = no end.</summary>
    public DateTime? ModifiedTo { get; set; } = null;

    // --- File names ---

    /// <summary>File name patterns, space separated (e.g. "*.cs *.md").</summary>
    public string FileNames { get; set; } = "*.*";

    /// <summary>The file names are regular expressions.</summary>
    public bool FileNamesAsRegex { get; set; } = false;

    /// <summary>Search inside ZIP archives.</summary>
    public bool SearchInsideZipArchives { get; set; } = false;

    /// <summary>Skip binary files.</summary>
    public bool SkipBinaryFiles { get; set; } = false;

    /// <summary>Search only binary files.</summary>
    public bool OnlyBinaryFiles { get; set; } = false;

    /// <summary>Skip files with the extensions in <see cref="SkippedExtensions"/>.</summary>
    public bool SkipByExtension { get; set; } = false;

    /// <summary>Extensions to skip, space separated (e.g. "*.exe *.dll").</summary>
    public string SkippedExtensions { get; set; } = "";

    // --- Folder name ---

    /// <summary>Folder names to search in, space separated (names with spaces in quotes).</summary>
    public string FolderName { get; set; } = "";

    /// <summary>Search in subfolders too.</summary>
    public bool IncludeSubfolders { get; set; } = true;

    /// <summary>The folder names are regular expressions.</summary>
    public bool FolderNamesAsRegex { get; set; } = false;

    /// <summary>What the folder regular expression is matched against.</summary>
    public RegexTarget RegularExpressionOn { get; set; } = RegexTarget.PathAndFileName;

    /// <summary>Filter folders by a regular expression on their base (last) name.</summary>
    public bool BaseNameAsRegex { get; set; } = false;

    /// <summary>Regular expression for <see cref="BaseNameAsRegex"/>.</summary>
    public string BaseNamePattern { get; set; } = "";

    /// <summary>Skip the folders in <see cref="SkippedFolders"/>.</summary>
    public bool SkipFolders { get; set; } = false;

    /// <summary>Folder names to skip, space separated (e.g. ".git bin obj").</summary>
    public string SkippedFolders { get; set; } = "";

    // --- General ---

    /// <summary>Count the files before searching to show progress in %.</summary>
    public bool CountFilesFirst { get; set; } = false;

    /// <summary>New query with the defaults, "Count files first" taken from the settings.</summary>
    public static SearchQuery CreateDefault(AppConfig config) => new() { CountFilesFirst = config.ShowProgressInPercent };

    /// <summary>Independent deep copy.</summary>
    public SearchQuery Copy() =>
        JsonSerializer.Deserialize<SearchQuery>(JsonSerializer.Serialize(this))!;
}

/// <summary>What a folder regular expression is matched against.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RegexTarget>))]
public enum RegexTarget
{
    /// <summary>Full path including the file name.</summary>
    PathAndFileName,

    /// <summary>Folder path only.</summary>
    PathName,
}

/// <summary>Time unit of the "modified within last" file date filter.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TimeUnit>))]
public enum TimeUnit
{
    /// <summary>Seconds.</summary>
    Seconds,

    /// <summary>Minutes.</summary>
    Minutes,

    /// <summary>Hours.</summary>
    Hours,

    /// <summary>Days.</summary>
    Days,

    /// <summary>Weeks.</summary>
    Weeks,

    /// <summary>Months.</summary>
    Months,

    /// <summary>Years.</summary>
    Years,
}
