using System.Text.Json;

namespace SaveAndLoad;

/// <summary>Saves and loads <see cref="IStorable"/> objects as JSON files.</summary>
public sealed class ObjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly StorageFolders _folders;

    /// <summary>Creates a store working in the given folders.</summary>
    public ObjectStore(StorageFolders folders) => _folders = folders;

    /// <summary>Folders the store works in.</summary>
    public StorageFolders Folders => _folders;

    /// <summary>Full path of the user file of <typeparamref name="T"/>.</summary>
    public string UserFilePath<T>() where T : IStorable =>
        Path.Combine(_folders.UserFolder, T.FileName);

    /// <summary>Full path of the standard data file of <typeparamref name="T"/>.</summary>
    public string DefaultsFilePath<T>() where T : IStorable =>
        Path.Combine(_folders.DefaultsFolder, T.FileName);

    /// <summary>
    /// Loads T from the user file, else from the defaults file, else returns a new T.
    /// Never throws: a broken file is reported in <see cref="LoadResult{T}.Problem"/>.
    /// </summary>
    public LoadResult<T> Load<T>() where T : IStorable, new()
    {
        string? problem = null;

        string userFile = UserFilePath<T>();
        if (File.Exists(userFile))
        {
            T? fromUser = TryRead<T>(userFile, ref problem);
            if (fromUser is not null)
            {
                return new LoadResult<T>(fromUser, LoadSource.UserFile, null);
            }
        }

        string defaultsFile = DefaultsFilePath<T>();
        if (File.Exists(defaultsFile))
        {
            T? fromDefaults = TryRead<T>(defaultsFile, ref problem);
            if (fromDefaults is not null)
            {
                return new LoadResult<T>(fromDefaults, LoadSource.DefaultsFile, problem);
            }
        }

        return new LoadResult<T>(new T(), LoadSource.NewObject, problem);
    }

    /// <summary>
    /// Writes the object to its user file. Creates the folder if needed. Writes a temp file
    /// first, so a crash never leaves a half-written file. Throws IOException on failure.
    /// </summary>
    public void Save<T>(T value) where T : IStorable
    {
        Directory.CreateDirectory(_folders.UserFolder);
        string userFile = UserFilePath<T>();
        string tempFile = userFile + ".tmp";
        File.WriteAllText(tempFile, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(tempFile, userFile, overwrite: true);
    }

    private static T? TryRead<T>(string path, ref string? problem)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            problem ??= $"Could not read '{path}': {ex.Message}";
            return default;
        }
    }
}
