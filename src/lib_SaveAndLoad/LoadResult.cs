namespace SaveAndLoad;

/// <summary>Where a loaded object came from.</summary>
public enum LoadSource
{
    /// <summary>Read from the user file.</summary>
    UserFile,

    /// <summary>Read from the standard data file in the defaults folder.</summary>
    DefaultsFile,

    /// <summary>No readable file: a new object with its built-in default values.</summary>
    NewObject,
}

/// <summary>Result of <see cref="ObjectStore.Load{T}"/>.</summary>
/// <param name="Value">The loaded object, never null.</param>
/// <param name="Source">Where it came from.</param>
/// <param name="Problem">Text if a file existed but could not be read, else null.</param>
public sealed record LoadResult<T>(T Value, LoadSource Source, string? Problem);
