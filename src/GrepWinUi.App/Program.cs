namespace GrepWinUi.App;

/// <summary>Application entry point.</summary>
internal static class Program
{
    /// <summary>Starts the WinForms application with the main window.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
