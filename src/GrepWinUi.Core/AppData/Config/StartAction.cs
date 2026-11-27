namespace GrepWinUi.Core.AppData.Config;

/// <summary>What the program does on start with the active result form (reopened or new).</summary>
public enum StartAction
{
    /// <summary>Nothing: the reopened form just shows its result.</summary>
    None,

    /// <summary>Open the search properties dialog for the form.</summary>
    OpenDialog,

    /// <summary>Run the form's search at once, without the dialog (Settings "Repeat last search").</summary>
    Search,
}

/// <summary>Decides the <see cref="StartAction"/> from the "On start of program" settings.</summary>
public static class StartActions
{
    /// <summary>
    /// "Repeat last search" wins when there is a search string to search for (old program: the first search form
    /// searches without dialog). Otherwise a reopened form opens the dialog only with "Open search form", and a
    /// new form (nothing reopened) always opens it.
    /// </summary>
    /// <param name="config">Settings ("On start of program").</param>
    /// <param name="reopened">True when result forms of the last exit were reopened.</param>
    /// <param name="searchString">Search string of the active form's query.</param>
    public static StartAction Decide(AppConfig config, bool reopened, string searchString)
    {
        if (config.RepeatLastSearchOnStart && searchString.Length > 0)
        {
            return StartAction.Search;
        }
        return !reopened || config.OpenSearchFormOnStart ? StartAction.OpenDialog : StartAction.None;
    }
}
