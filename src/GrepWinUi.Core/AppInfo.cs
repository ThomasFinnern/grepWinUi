namespace GrepWinUi.Core;

/// <summary>Static facts about the application (name, version).</summary>
public static class AppInfo
{
    /// <summary>Product name shown in window titles.</summary>
    public const string ProductName = "grepWinUi";

    /// <summary>Builds the main window title, e.g. "grepWinUi 1.0.0".</summary>
    /// <param name="version">Application version; only major.minor.build is shown.</param>
    public static string MainWindowTitle(Version version) =>
        $"{ProductName} {version.ToString(3)}";
}
