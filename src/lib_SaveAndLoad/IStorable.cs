namespace SaveAndLoad;

/// <summary>A class that can be saved and loaded by <see cref="ObjectStore"/>. It only defines its content.</summary>
public interface IStorable
{
    /// <summary>File name without folder, e.g. "Config.json".</summary>
    static abstract string FileName { get; }
}
