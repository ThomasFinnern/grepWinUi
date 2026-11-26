using System.Text.Json;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Saves and loads a <see cref="SearchResult"/> (File → Save / Open search result): the search parameters,
/// the file list and the matching lines with their hits, plus counts, time and problems.
/// JSON with a header (<see cref="Kind"/>, <see cref="Version"/>, saved date) so other JSON files are rejected.
/// </summary>
public static class SearchResultFile
{
    /// <summary>File extension of result files.</summary>
    public const string Extension = ".result.json";

    /// <summary>Filter for the file dialogs.</summary>
    public const string DialogFilter = "Search result files (*.result.json)|*.result.json|All files (*.*)|*.*";

    /// <summary>Marks a file as a grepWinUi search result.</summary>
    public const string Kind = "grepWinUi search result";

    /// <summary>Format version written into new files.</summary>
    public const int Version = 1;

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>
    /// The path with the extension <see cref="Extension"/>: kept when it is there, a plain ".json" is
    /// replaced, else it is added (e.g. "result" → "result.result.json").
    /// </summary>
    public static string WithExtension(string path)
    {
        if (path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }
        string stem = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? path[..^".json".Length] : path;
        return stem + Extension;
    }

    /// <summary>Writes <paramref name="result"/> to <paramref name="path"/> (temp file, then replace).</summary>
    /// <exception cref="IOException">The file can't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">No write access.</exception>
    public static void Save(SearchResult result, string path)
    {
        string json = JsonSerializer.Serialize(new Content { Saved = DateTime.Now, Result = result }, WriteOptions);
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>Reads a result file.</summary>
    /// <exception cref="InvalidDataException">The file is no grepWinUi search result or is broken.</exception>
    /// <exception cref="IOException">The file can't be read.</exception>
    /// <exception cref="UnauthorizedAccessException">No read access.</exception>
    public static SearchResult Load(string path)
    {
        Content? content;
        try
        {
            content = JsonSerializer.Deserialize<Content>(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"'{path}' is not a valid JSON file: {ex.Message}", ex);
        }

        if (content?.Kind != Kind || content.Result?.Query is null || content.Result.Files is null)
        {
            throw new InvalidDataException($"'{path}' is not a {Kind} file.");
        }
        return content.Result with { Problems = content.Result.Problems ?? [] };
    }

    /// <summary>File content: header plus the result.</summary>
    private sealed class Content
    {
        public string Kind { get; set; } = SearchResultFile.Kind;

        public int Version { get; set; } = SearchResultFile.Version;

        public DateTime Saved { get; set; }

        public SearchResult? Result { get; set; }
    }
}
