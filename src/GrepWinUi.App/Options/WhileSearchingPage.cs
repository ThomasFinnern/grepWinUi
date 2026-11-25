using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.App.Options;

/// <summary>Settings page "While searching".</summary>
internal sealed class WhileSearchingPage : OptionsPage
{
    private readonly CheckBox _showFileNames = Check("Show file names in status bar");
    private readonly CheckBox _progressInPercent = Check("Count files first and show progress in %");

    /// <summary>Creates the page controls.</summary>
    public WhileSearchingPage()
    {
        Controls.Add(Column(_showFileNames, _progressInPercent));
    }

    /// <inheritdoc/>
    public override string Title => "While searching";

    /// <inheritdoc/>
    public override void LoadFrom(AppConfig config)
    {
        _showFileNames.Checked = config.ShowFileNamesWhileSearching;
        _progressInPercent.Checked = config.ShowProgressInPercent;
    }

    /// <inheritdoc/>
    public override void SaveTo(AppConfig config)
    {
        config.ShowFileNamesWhileSearching = _showFileNames.Checked;
        config.ShowProgressInPercent = _progressInPercent.Checked;
    }
}
