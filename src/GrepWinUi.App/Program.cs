using GrepWinUi.Core;
using GrepWinUi.Core.AppData.Config;
using SaveAndLoad;

namespace GrepWinUi.App;

/// <summary>Application entry point.</summary>
internal static class Program
{
    /// <summary>Loads the user settings and starts the main window.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        ObjectStore store = new(StorageFolders.ForProduct(AppInfo.ProductName));
        LoadResult<AppConfig> config = store.Load<AppConfig>();
        if (config.Problem is not null)
        {
            MessageBox.Show(config.Problem, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        Application.Run(new MainForm(config.Value));
    }
}
