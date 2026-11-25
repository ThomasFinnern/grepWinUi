using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.App.Options;

/// <summary>Settings page "Search result view": standard view options of new result forms.</summary>
internal sealed class ResultViewPage : OptionsPage
{
    private readonly NumericUpDown _keepBefore = Number();
    private readonly NumericUpDown _keepAfter = Number();
    private readonly NumericUpDown _showBefore = Number();
    private readonly NumericUpDown _showAfter = Number();
    private readonly CheckBox _title = Check("Title");
    private readonly CheckBox _lineNumbers = Check("Line numbers");
    private readonly CheckBox _fixedFont = Check("Fixed font");
    private readonly CheckBox _fileNames = Check("File names");
    private readonly CheckBox _fileContents = Check("File contents");
    private readonly CheckBox _wholeLine = Check("Whole line");
    private readonly CheckBox _doubleBlanks = Check("Double blanks");

    /// <summary>Creates the page controls.</summary>
    public ResultViewPage()
    {
        TableLayoutPanel lines = new() { AutoSize = true, ColumnCount = 2 };
        AddRow(lines, "Keep lines before match", _keepBefore);
        AddRow(lines, "Keep lines after match", _keepAfter);
        AddRow(lines, "Show lines before match", _showBefore);
        AddRow(lines, "Show lines after match", _showAfter);

        Controls.Add(Column(lines, _title, _lineNumbers, _fixedFont, _fileNames, _fileContents, _wholeLine, _doubleBlanks));
    }

    /// <inheritdoc/>
    public override string Title => "Search result view";

    /// <inheritdoc/>
    public override void LoadFrom(AppConfig config)
    {
        ResultViewOptions view = config.ResultView;
        _keepBefore.Value = Clamp(view.KeepLinesBefore);
        _keepAfter.Value = Clamp(view.KeepLinesAfter);
        _showBefore.Value = Clamp(view.ShowLinesBefore);
        _showAfter.Value = Clamp(view.ShowLinesAfter);
        _title.Checked = view.ShowTitle;
        _lineNumbers.Checked = view.ShowLineNumbers;
        _fixedFont.Checked = view.FixedFont;
        _fileNames.Checked = view.ShowFileNames;
        _fileContents.Checked = view.ShowFileContents;
        _wholeLine.Checked = view.ShowWholeLine;
        _doubleBlanks.Checked = view.ShowDoubleBlanks;
    }

    /// <inheritdoc/>
    public override void SaveTo(AppConfig config)
    {
        ResultViewOptions view = config.ResultView;
        view.KeepLinesBefore = (int)_keepBefore.Value;
        view.KeepLinesAfter = (int)_keepAfter.Value;
        view.ShowLinesBefore = (int)_showBefore.Value;
        view.ShowLinesAfter = (int)_showAfter.Value;
        view.ShowTitle = _title.Checked;
        view.ShowLineNumbers = _lineNumbers.Checked;
        view.FixedFont = _fixedFont.Checked;
        view.ShowFileNames = _fileNames.Checked;
        view.ShowFileContents = _fileContents.Checked;
        view.ShowWholeLine = _wholeLine.Checked;
        view.ShowDoubleBlanks = _doubleBlanks.Checked;
    }

    private static NumericUpDown Number() => new() { Minimum = 0, Maximum = 99, Width = 60 };

    private static decimal Clamp(int value) => Math.Clamp(value, 0, 99);

    private static void AddRow(TableLayoutPanel table, string label, Control control)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left });
        table.Controls.Add(control);
    }
}
