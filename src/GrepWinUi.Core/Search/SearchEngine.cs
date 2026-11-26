using System.Diagnostics;
using System.Text;
using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Runs a search on a background thread: finds the files (<see cref="FileFinder"/>) and the matching
/// lines (<see cref="LineMatcher"/>). Cancelling returns what was found so far (no exception).
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
    public static Task<SearchResult> RunAsync(SearchQuery query, IProgress<SearchProgress>? progress, CancellationToken cancel) =>
        Task.Run(() => Run(query.Copy(), progress, cancel), CancellationToken.None);

    /// <summary>Searches on the calling thread (used by <see cref="RunAsync"/> and tests).</summary>
    public static SearchResult Run(SearchQuery query, IProgress<SearchProgress>? progress, CancellationToken cancel)
    {
        Stopwatch time = Stopwatch.StartNew();
        if (query.SearchString.Length == 0)
        {
            return new SearchResult(query, [], 0, 0, 0, time.Elapsed, Cancelled: false, ["Search string is empty."]);
        }

        LineMatcher matcher = LineMatcher.PlainText(query.SearchString);
        FileFinder finder = new();
        List<FileResult> files = [];
        List<string> problems = [];
        int filesSearched = 0;
        int matchCount = 0;
        TimeSpan lastReport = TimeSpan.Zero;

        foreach (string path in finder.Find(query.FolderName, query.FileNames, query.IncludeSubfolders, cancel))
        {
            filesSearched++;
            if (time.Elapsed - lastReport >= ProgressInterval)
            {
                lastReport = time.Elapsed;
                progress?.Report(new SearchProgress(filesSearched, finder.FoldersSearched, files.Count, path));
            }

            FileResult? file = SearchFile(path, matcher, problems, cancel);
            if (file is not null)
            {
                files.Add(file);
                matchCount += file.Lines.Sum(line => line.Hits.Count);
            }
            if (cancel.IsCancellationRequested)
            {
                break;
            }
        }

        progress?.Report(new SearchProgress(filesSearched, finder.FoldersSearched, files.Count, ""));
        return new SearchResult(
            query, files, filesSearched, finder.FoldersSearched, matchCount, time.Elapsed,
            cancel.IsCancellationRequested, [.. finder.Problems, .. problems]);
    }

    /// <summary>
    /// Matching lines of one file, or null when it has none. Read as UTF-8; UTF-16 / UTF-32 files are
    /// detected by their byte order mark. An unreadable file is noted in <paramref name="problems"/>.
    /// </summary>
    public static FileResult? SearchFile(string path, LineMatcher matcher, List<string> problems, CancellationToken cancel)
    {
        List<LineMatch> lines = [];
        try
        {
            using StreamReader reader = new(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            int lineNumber = 0;
            while (reader.ReadLine() is string line)
            {
                lineNumber++;
                IReadOnlyList<Hit> hits = matcher.Find(line);
                if (hits.Count > 0)
                {
                    lines.Add(new LineMatch(lineNumber, line, hits));
                }
                if (cancel.IsCancellationRequested)
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
        return lines.Count > 0 ? new FileResult(path, lines) : null;
    }
}
