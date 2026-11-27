using GrepWinUi.Core.AppData.Config;
using static GrepWinUi.App.Search.SearchFormControls;

namespace GrepWinUi.App.Options;

/// <summary>Settings page "On start of program".</summary>
internal sealed class OnStartPage : OptionsPage
{
    private readonly CheckBox _openSearchForm = Check("Open search form");
    private readonly CheckBox _repeatLastSearch = NoEffect(Check("Repeat last search"));
    private readonly CheckBox _reopenLastSearches = Check("Reopen last open searches");
    private readonly CheckBox _showLastResult = Check("Show last search result");

    /// <summary>Creates the page controls.</summary>
    public OnStartPage()
    {
        Controls.Add(Column(_openSearchForm, _repeatLastSearch, _reopenLastSearches, _showLastResult));
    }

    /// <inheritdoc/>
    public override string Title => "On start of program";

    /// <inheritdoc/>
    public override void LoadFrom(AppConfig config)
    {
        _openSearchForm.Checked = config.OpenSearchFormOnStart;
        _repeatLastSearch.Checked = config.RepeatLastSearchOnStart;
        _reopenLastSearches.Checked = config.ReopenLastSearchesOnStart;
        _showLastResult.Checked = config.ShowLastResultOnStart;
    }

    /// <inheritdoc/>
    public override void SaveTo(AppConfig config)
    {
        config.OpenSearchFormOnStart = _openSearchForm.Checked;
        config.RepeatLastSearchOnStart = _repeatLastSearch.Checked;
        config.ReopenLastSearchesOnStart = _reopenLastSearches.Checked;
        config.ShowLastResultOnStart = _showLastResult.Checked;
    }
}
