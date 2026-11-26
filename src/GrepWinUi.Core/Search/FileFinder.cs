using System.IO.Enumeration;
using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Finds the files to search: the folders of "Folder name" (<see cref="FolderList"/>, each with its
/// subfolders if wanted), filtered by <see cref="FileFilter"/> (file names as wildcards like "*.cs *.md"
/// with Windows rules or as RegEx, skip by extension, skip folders, folder names / base name as RegEx).
/// Each file comes once, files of a folder before its subfolders, both sorted by name. Missing folders
/// and folders that can't be read are skipped and noted in <see cref="Problems"/>.
/// </summary>
public sealed class FileFinder
{
    private readonly List<string> _problems = [];

    /// <summary>Folders looked into so far.</summary>
    public int FoldersSearched { get; private set; }

    /// <summary>Texts about folders that were missing or could not be read.</summary>
    public IReadOnlyList<string> Problems => _problems;

    /// <summary>Splits the file names text into patterns; empty text means all files.</summary>
    public static IReadOnlyList<string> Patterns(string fileNames)
    {
        string[] patterns = fileNames.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return patterns.Length > 0 ? patterns : ["*"];
    }

    /// <summary>True when <paramref name="fileName"/> (name only) matches one of the patterns.</summary>
    public static bool Matches(string fileName, IReadOnlyList<string> patterns) =>
        patterns.Any(pattern => FileSystemName.MatchesWin32Expression(
            FileSystemName.TranslateWin32Expression(pattern), fileName, ignoreCase: true));

    /// <summary>Files in <paramref name="folder"/> matching <paramref name="fileNames"/> (no other filters).</summary>
    public IEnumerable<string> Find(string folder, string fileNames, bool includeSubfolders, CancellationToken cancel) =>
        Find(new SearchQuery { FolderName = folder, FileNames = fileNames, IncludeSubfolders = includeSubfolders }, cancel);

    /// <summary>Files of the folders and filters of <paramref name="query"/>.</summary>
    /// <exception cref="ArgumentException">A RegEx of the filters is not valid (thrown at once, not while iterating).</exception>
    public IEnumerable<string> Find(SearchQuery query, CancellationToken cancel)
    {
        FileFilter filter = FileFilter.Create(query);
        IReadOnlyList<string> folders = FolderList.Split(query.FolderName);
        if (folders.Count == 0)
        {
            _problems.Add("No folder given.");
            return [];
        }

        List<string> roots = [];
        foreach (string folder in folders)
        {
            if (Directory.Exists(folder))
            {
                roots.Add(folder);
            }
            else
            {
                _problems.Add($"Folder not found: '{folder}'");
            }
        }
        return FindInAll(roots, filter, query.IncludeSubfolders, cancel);
    }

    /// <summary>Files of all start folders; a file found from two overlapping start folders comes once.</summary>
    private IEnumerable<string> FindInAll(List<string> roots, FileFilter filter, bool includeSubfolders, CancellationToken cancel)
    {
        HashSet<string> found = new(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots)
        {
            string fullRoot = Path.GetFullPath(root);
            foreach (string file in FindIn(fullRoot, fullRoot, filter, includeSubfolders, cancel))
            {
                if (found.Add(file))
                {
                    yield return file;
                }
            }
        }
    }

    private IEnumerable<string> FindIn(string root, string folder, FileFilter filter, bool includeSubfolders, CancellationToken cancel)
    {
        if (cancel.IsCancellationRequested)
        {
            yield break;
        }

        FoldersSearched++;
        if (filter.SearchesFilesIn(folder))
        {
            string relative = Path.GetRelativePath(root, folder);
            relative = relative == "." ? "" : relative;
            foreach (string file in Read(folder, Directory.EnumerateFiles))
            {
                if (cancel.IsCancellationRequested)
                {
                    yield break;
                }
                if (filter.Accepts(relative, Path.GetFileName(file)))
                {
                    yield return file;
                }
            }
        }

        if (!includeSubfolders)
        {
            yield break;
        }
        foreach (string subfolder in Read(folder, Directory.EnumerateDirectories))
        {
            if (filter.SkipsFolder(subfolder))
            {
                continue;
            }
            foreach (string file in FindIn(root, subfolder, filter, includeSubfolders, cancel))
            {
                yield return file;
            }
        }
    }

    /// <summary>Entries of a folder sorted by name; a folder that can't be read gives none (problem noted).</summary>
    private List<string> Read(string folder, Func<string, IEnumerable<string>> entries)
    {
        try
        {
            List<string> result = [.. entries(folder)];
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _problems.Add($"Folder not readable: '{folder}' ({ex.Message})");
            return [];
        }
    }
}
