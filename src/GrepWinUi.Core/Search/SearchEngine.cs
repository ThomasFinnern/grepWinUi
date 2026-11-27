using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Runs a search on a background thread: finds the files (<see cref="FileFinder"/>) and the matching
/// lines (<see cref="LineMatcher"/>). With <see cref="SearchQuery.CountFilesFirst"/> all files are found
/// and counted first, so the progress knows the total (progress in %).
/// Cancelling returns what was found so far (no exception).
/// Basic version: one folder, file name wildcards, plain text ignoring case.
/// </summary>
public static class SearchEngine
{
    /// <summary>Shortest time between two progress reports.</summary>
    public static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>Searches <paramref name="query"/> without blocking the caller.</summary>
    /// <param name="query">What and where to search; a copy is kept in the result.</param>
    /// <param name="progress">Gets a report at most every <see cref="ProgressInterval"/> and at the end.</param>
    /// <param name="cancel">Stops the search; the result then has <see cref="SearchResult.Cancelled"/> set.</param>
    /// <param name="keep">Lines kept before / after each matching line (<see cref="FileResult.ContextLines"/>).</param>
    /// <param name="onlyFiles">
    /// Files to search instead of the folders of the query ("Only in files of current result", <see cref="SearchScope"/>):
    /// read in this order, without folder walk and file / folder filters (as in the old program); a missing file
    /// is a problem. Null: the folders and filters of the query.
    /// </param>
    public static Task<SearchResult> RunAsync(
        SearchQuery query, IProgress<SearchProgress>? progress, CancellationToken cancel, KeepLines keep = default,
        IReadOnlyList<string>? onlyFiles = null) =>
        Task.Run(() => Run(query.Copy(), progress, cancel, keep, onlyFiles), CancellationToken.None);

    /// <summary>Searches on the calling thread (used by <see cref="RunAsync"/> and tests).</summary>
    public static SearchResult Run(
        SearchQuery query, IProgress<SearchProgress>? progress, CancellationToken cancel, KeepLines keep = default,
        IReadOnlyList<string>? onlyFiles = null)
    {
        Stopwatch time = Stopwatch.StartNew();
        if (query.SearchString.Length == 0)
        {
            return new SearchResult(query, [], 0, 0, 0, time.Elapsed, Cancelled: false, ["Search string is empty."]);
        }

        LineMatcher matcher;
        try
        {
            matcher = LineMatcher.Create(query);
        }
        catch (ArgumentException ex)
        {
            return new SearchResult(query, [], 0, 0, 0, time.Elapsed, Cancelled: false, [$"Invalid regular expression: {ex.Message}"]);
        }
        FileFinder finder = new();
        List<FileResult> files = [];
        List<string> problems = [];
        int filesSearched = 0;
        int matchCount = 0;
        TimeSpan lastReport = TimeSpan.Zero;

        IEnumerable<string> paths;
        if (onlyFiles is not null)
        {
            paths = ExistingFiles(onlyFiles, problems);
        }
        else
        {
            try
            {
                paths = finder.Find(query, cancel);
            }
            catch (ArgumentException ex)
            {
                return new SearchResult(query, [], 0, 0, 0, time.Elapsed, Cancelled: false, [$"Invalid regular expression: {ex.Message}"]);
            }
        }
        ResultMode mode = ResultMode.Of(query);
        int? totalFiles = null;
        if (query.CountFilesFirst)
        {
            List<string> counted = CountFiles(paths, finder, progress, time, cancel);
            totalFiles = counted.Count;
            paths = counted;
        }

        foreach (string path in paths)
        {
            if (cancel.IsCancellationRequested)
            {
                break;
            }
            filesSearched++;
            if (time.Elapsed - lastReport >= ProgressInterval)
            {
                lastReport = time.Elapsed;
                progress?.Report(new SearchProgress(filesSearched, finder.FoldersSearched, files.Count, path, totalFiles));
            }

            FileKind kind;
            try
            {
                kind = FileKind.DetectFile(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                problems.Add($"File not readable: '{path}' ({ex.Message})");
                continue;
            }
            if ((query.SkipBinaryFiles && kind.IsBinary) || (query.OnlyBinaryFiles && !kind.IsBinary))
            {
                continue;
            }

            FileResult? file = SearchFile(path, matcher, problems, cancel, keep, mode, kind);
            if (file is not null)
            {
                files.Add(file);
                matchCount += mode.CountOf(file);
            }
            if (cancel.IsCancellationRequested)
            {
                break;
            }
        }

        progress?.Report(new SearchProgress(filesSearched, finder.FoldersSearched, files.Count, "", totalFiles));
        return new SearchResult(
            query, files, filesSearched, finder.FoldersSearched, matchCount, time.Elapsed,
            cancel.IsCancellationRequested, [.. finder.Problems, .. problems], InFilesOfPreviousResult: onlyFiles is not null);
    }

    /// <summary>The given files that still exist, each once (case ignored); a missing one is noted in <paramref name="problems"/>.</summary>
    private static List<string> ExistingFiles(IReadOnlyList<string> files, List<string> problems)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<string> existing = [];
        foreach (string file in files)
        {
            if (!seen.Add(file))
            {
                continue;
            }
            if (File.Exists(file))
            {
                existing.Add(file);
            }
            else
            {
                problems.Add($"File not found: '{file}'");
            }
        }
        return existing;
    }

    /// <summary>
    /// "Count files first": finds all files before the search. Reports progress with
    /// <see cref="SearchProgress.Counting"/> set (at the start, then at most every <see cref="ProgressInterval"/>).
    /// </summary>
    private static List<string> CountFiles(
        IEnumerable<string> paths, FileFinder finder, IProgress<SearchProgress>? progress, Stopwatch time, CancellationToken cancel)
    {
        List<string> files = [];
        TimeSpan lastReport = time.Elapsed;
        progress?.Report(new SearchProgress(0, 0, 0, "", TotalFiles: 0, Counting: true));
        foreach (string path in paths)
        {
            files.Add(path);
            if (time.Elapsed - lastReport >= ProgressInterval)
            {
                lastReport = time.Elapsed;
                progress?.Report(new SearchProgress(0, finder.FoldersSearched, 0, path, files.Count, Counting: true));
            }
            if (cancel.IsCancellationRequested)
            {
                break;
            }
        }
        return files;
    }

    /// <summary>
    /// Matching lines of one file, or null when it has none. Read with the encoding of <paramref name="kind"/>
    /// (null: <see cref="FileKind.DetectFile"/>); in a binary file control characters become '·'. Very long
    /// lines are cut (<see cref="LongLines"/>). An unreadable file, or one where the regular expression takes
    /// too long for a line (<see cref="LineMatcher.RegexTimeout"/>), is noted in <paramref name="problems"/>
    /// and gives null. Keeps up to <paramref name="keep"/> lines before / after each found line:
    /// the last lines before wait in a small queue and are only stored when a found line follows.
    /// <paramref name="mode"/>: a found line is a line with a hit, or without one ("Non matching lines",
    /// no hits then). "First match per file only" keeps the first found line, reads on only for its lines
    /// after, then stops. "List files without match" gives a file without lines only when nothing was found;
    /// "File names only" one without lines when something was found; both stop at the first found line.
    /// </summary>
    public static FileResult? SearchFile(
        string path, LineMatcher matcher, List<string> problems, CancellationToken cancel,
        KeepLines keep = default, ResultMode mode = default, FileKind? kind = null)
    {
        List<LineMatch> lines = [];
        List<ContextLine> context = [];
        Queue<ContextLine> waiting = new();
        int afterLeft = 0;
        bool foundAny = false;
        try
        {
            FileKind fileKind = kind ?? FileKind.DetectFile(path);
            using StreamReader reader = new(path, fileKind.Encoding, detectEncodingFromByteOrderMarks: !fileKind.IsBinary);
            int lineNumber = 0;
            while (reader.ReadLine() is string read)
            {
                string line = fileKind.IsBinary ? FileKind.ShowControlCharacters(read) : read;
                lineNumber++;
                IReadOnlyList<Hit> hits = matcher.Find(line);
                bool found = (hits.Count > 0) != mode.NonMatchingLines;
                bool takeLine = found && !(mode.FirstMatchOnly && lines.Count > 0);
                if (found && mode.NoLines)
                {
                    foundAny = true;
                    break;
                }
                if (takeLine)
                {
                    lines.Add(LongLines.Cut(new LineMatch(lineNumber, line, mode.NonMatchingLines ? [] : hits)));
                    context.AddRange(waiting);
                    waiting.Clear();
                    afterLeft = keep.After;
                }
                else if (afterLeft > 0)
                {
                    context.Add(new ContextLine(lineNumber, LongLines.Cut(line)));
                    afterLeft--;
                }
                else if (keep.Before > 0)
                {
                    waiting.Enqueue(new ContextLine(lineNumber, LongLines.Cut(line)));
                    if (waiting.Count > keep.Before)
                    {
                        waiting.Dequeue();
                    }
                }
                if (cancel.IsCancellationRequested || (mode.FirstMatchOnly && lines.Count > 0 && afterLeft == 0))
                {
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problems.Add($"File not readable: '{path}' ({ex.Message})");
            return null;
        }
        catch (RegexMatchTimeoutException)
        {
            problems.Add($"Regular expression too slow, file skipped: '{path}'");
            return null;
        }
        if (mode.FilesWithoutMatch)
        {
            return foundAny ? null : new FileResult(path, []);
        }
        if (mode.FileNamesOnly)
        {
            return foundAny ? new FileResult(path, []) : null;
        }
        return lines.Count > 0 ? new FileResult(path, lines, context) : null;
    }
}
