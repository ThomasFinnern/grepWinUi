namespace GrepWinUi.Core;

/// <summary>Builds menu item texts. Items without a working function are marked with "*".</summary>
public static class MenuText
{
    /// <summary>Marker put in front of menu texts whose function is not implemented yet.</summary>
    public const string NotImplementedMarker = "*";

    /// <summary>Returns the text to show for a menu item.</summary>
    /// <param name="text">Plain menu text, e.g. "Open search query".</param>
    /// <param name="isImplemented">False adds the <see cref="NotImplementedMarker"/> in front.</param>
    public static string Display(string text, bool isImplemented) =>
        isImplemented ? text : NotImplementedMarker + text;
}
