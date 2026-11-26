using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>Result of one search.</summary>
/// <param name="Query">Copy of the query that was searched.</param>
/// <param name="Files">Files with at least one matching line, in search order.</param>
/// <param name="FilesSearched">Number of files read.</param>
/// <param name="FoldersSearched">Number of folders looked into.</param>
/// <param name="MatchCount">Number of hits in all files.</param>
/// <param name="Duration">Time the search took.</param>
/// <param name="Cancelled">True when the search was stopped; the result holds what was found until then.</param>
/// <param name="Problems">Texts about things that went wrong (missing folder, unreadable file, …).</param>
public sealed record SearchResult(
    SearchQuery Query,
    IReadOnlyList<FileResult> Files,
    int FilesSearched,
    int FoldersSearched,
    int MatchCount,
    TimeSpan Duration,
    bool Cancelled,
    IReadOnlyList<string> Problems);

/// <summary>A file with its matching lines.</summary>
/// <param name="Path">Full path of the file.</param>
/// <param name="Lines">Matching lines in file order.</param>
public sealed record FileResult(string Path, IReadOnlyList<LineMatch> Lines);

/// <summary>A matching line.</summary>
/// <param name="LineNumber">Line number, the first line is 1.</param>
/// <param name="Text">Text of the line.</param>
/// <param name="Hits">All hits in the line, left to right.</param>
public sealed record LineMatch(int LineNumber, string Text, IReadOnlyList<Hit> Hits);

/// <summary>One hit inside a line.</summary>
/// <param name="Start">Index of the first character (0-based).</param>
/// <param name="Length">Number of characters.</param>
public sealed record Hit(int Start, int Length);

/// <summary>Progress of a running search.</summary>
/// <param name="FilesSearched">Files read so far.</param>
/// <param name="FoldersSearched">Folders looked into so far.</param>
/// <param name="FilesMatched">Files with at least one match so far.</param>
/// <param name="CurrentFile">File being read (while counting: the file just found).</param>
/// <param name="TotalFiles">
/// All files to search, known when the files were counted first ("Count files first"); null otherwise.
/// While <paramref name="Counting"/>: the files counted so far.
/// </param>
/// <param name="Counting">True while the files are counted before the search.</param>
public sealed record SearchProgress(
    int FilesSearched, int FoldersSearched, int FilesMatched, string CurrentFile,
    int? TotalFiles = null, bool Counting = false);
