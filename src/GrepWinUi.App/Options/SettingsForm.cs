using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.App.Options;

/// <summary>Dialog to edit the user settings. Works on a copy; <see cref="Result"/> is set on Save.</summary>
internal sealed class SettingsForm : Form
{
    private readonly OptionsPage[] _pages =
    [
        new ResultViewPage(),
        new WhileSearchingPage(),
        new OnStartPage(),
        new TokenListPage("Search string token", config => config.SearchStringTokens),
        new TokenListPage("File name token", config => config.FileNameTokens),
        new TokenListPage("Folder name token", config => config.FolderNameTokens),
    ];
    private readonly ListBox _pageList = new() { Dock = DockStyle.Left, Width = 180, IntegralHeight = false };
    private readonly Label _pageTitle = new() { Dock = DockStyle.Top, Height = 28, Font = new Font(SystemFonts.MessageBoxFont!, FontStyle.Bold) };
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill };
    private readonly AppConfig _edited;

    /// <summary>Creates the dialog for a copy of <paramref name="config"/>.</summary>
    public SettingsForm(AppConfig config)
    {
        _edited = config.Copy();

        Text = "Settings";
        Size = new Size(620, 460);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;

        Button save = new() { Text = "Save", AutoSize = true };
        Button cancel = new() { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => OnSave();
        AcceptButton = save;
        CancelButton = cancel;

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(8),
        };
        buttons.Controls.AddRange([cancel, save]);

        Panel right = new() { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 8, 8) };
        right.Controls.Add(_pageHost);
        right.Controls.Add(_pageTitle);

        Controls.Add(right);
        Controls.Add(_pageList);
        Controls.Add(buttons);

        foreach (OptionsPage page in _pages)
        {
            page.LoadFrom(_edited);
            page.Dock = DockStyle.Fill;
            _pageList.Items.Add(page.Title);
        }
        _pageList.SelectedIndexChanged += (_, _) => ShowPage(_pages[_pageList.SelectedIndex]);
        _pageList.SelectedIndex = 0;
    }

    /// <summary>The edited settings after Save, else null.</summary>
    public AppConfig? Result { get; private set; }

    private void ShowPage(OptionsPage page)
    {
        _pageTitle.Text = page.Title;
        _pageHost.Controls.Clear();
        _pageHost.Controls.Add(page);
    }

    private void OnSave()
    {
        foreach (OptionsPage page in _pages)
        {
            page.SaveTo(_edited);
        }
        Result = _edited;
        DialogResult = DialogResult.OK;
    }
}
