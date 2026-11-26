using System.Text.Json;

namespace GrepWinUi.Core.AppData.Search;

/// <summary>
/// Saves and loads a single <see cref="SearchQuery"/> as a JSON file (File → Save / Open / Load search query).
/// The query is wrapped in a header (<see cref="Kind"/>, <see cref="Version"/>) so other JSON files are rejected.
/// </summary>
public static class SearchQueryFile
{
    /// <summary>File extension of query files.</summary>
    public const string Extension = ".json";

    /// <summary>Filter for the file dialogs.</summary>
    public const string DialogFilter = "Search query files (*.json)|*.json|All files (*.*)|*.*";

    /// <summary>Marks a file as a grepWinUi search query.</summary>
    public const string Kind = "grepWinUi search query";

    /// <summary>Format version written into new files.</summary>
    public const int Version = 1;

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>Writes <paramref name="query"/> to <paramref name="path"/> (temp file, then replace).</summary>
    /// <exception cref="IOException">The file can't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">No write access.</exception>
    public static void Save(SearchQuery query, string path)
    {
        string json = JsonSerializer.Serialize(new Content { Query = query }, WriteOptions);
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>Reads a query file.</summary>
    /// <exception cref="InvalidDataException">The file is no grepWinUi search query or is broken.</exception>
    /// <exception cref="IOException">The file can't be read.</exception>
    /// <exception cref="UnauthorizedAccessException">No read access.</exception>
    public static SearchQuery Load(string path)
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

        if (content?.Kind != Kind || content.Query is null)
        {
            throw new InvalidDataException($"'{path}' is not a {Kind} file.");
        }
        return content.Query;
    }

    /// <summary>File content: header plus the query.</summary>
    private sealed class Content
    {
        public string Kind { get; set; } = SearchQueryFile.Kind;

        public int Version { get; set; } = SearchQueryFile.Version;

        public SearchQuery? Query { get; set; }
    }
}
