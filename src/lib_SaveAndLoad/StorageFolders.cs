namespace SaveAndLoad;

/// <summary>Folders used for stored objects.</summary>
/// <param name="UserFolder">Per-user files, read and written (no admin rights needed).</param>
/// <param name="DefaultsFolder">Standard data file for each stored class, only read.</param>
public sealed record StorageFolders(string UserFolder, string DefaultsFolder)
{
    /// <summary>%AppData%\&lt;product&gt; for the user, &lt;exe folder&gt;\Defaults for standard data.</summary>
    /// <param name="productName">Application name, used as user folder name.</param>
    public static StorageFolders ForProduct(string productName) => new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), productName),
        Path.Combine(AppContext.BaseDirectory, "Defaults"));
}
