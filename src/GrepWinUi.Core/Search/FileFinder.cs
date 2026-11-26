using System.IO.Enumeration;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Finds the files to search: one folder (and its subfolders), file names as space separated wildcards
/// (e.g. "*.cs *.md", Windows rules: "*.*" means all files, case is ignored). Each file comes once,
/// files of a folder before its subfolders, both sorted by name. Folders that can't be read are skipped
/// and noted in <see cref="Problems"/>.
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

    /// <summary>Removes surrounding spaces and quotes of a folder name.</summary>
    public static string CleanFolder(string folder) => folder.Trim().Trim('"').Trim();

    /// <summary>Files in <paramref name="folder"/> matching <paramref name="fileNames"/>.</summary>
    public IEnumerable<string> Find(string folder, string fileNames, bool includeSubfolders, CancellationToken cancel)
    {
        string root = CleanFolder(folder);
        if (root.Length == 0)
        {
            _problems.Add("No folder given.");
            return [];
        }
        if (!Directory.Exists(root))
        {
            _problems.Add($"Folder not found: '{root}'");
            return [];
        }
        return FindIn(root, Patterns(fileNames), includeSubfolders, cancel);
    }

    private IEnumerable<string> FindIn(string folder, IReadOnlyList<string> patterns, bool includeSubfolders, CancellationToken cancel)
    {
        if (cancel.IsCancellationRequested)
        {
            yield break;
        }

        FoldersSearched++;
        foreach (string file in Read(folder, Directory.EnumerateFiles))
        {
            if (cancel.IsCancellationRequested)
            {
                yield break;
            }
            if (Matches(Path.GetFileName(file), patterns))
            {
                yield return file;
            }
        }

        if (!includeSubfolders)
        {
            yield break;
        }
        foreach (string subfolder in Read(folder, Directory.EnumerateDirectories))
        {
            foreach (string file in FindIn(subfolder, patterns, includeSubfolders, cancel))
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
