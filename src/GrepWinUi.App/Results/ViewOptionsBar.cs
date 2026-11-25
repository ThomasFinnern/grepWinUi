using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.App.Results;

/// <summary>Header line of a result form: view options (lines before/after, view switches). Wraps when narrow.</summary>
internal sealed class ViewOptionsBar : FlowLayoutPanel
{
    private readonly NumericUpDown _preLines = Number();
    private readonly NumericUpDown _postLines = Number();
    private readonly CheckBox _title = Check("Title");
    private readonly CheckBox _lineNumbers = Check("Line numbers");
    private readonly CheckBox _fixedFont = Check("Fixed font");
    private readonly CheckBox _fileNames = Check("File names");
    private readonly CheckBox _fileContents = Check("File contents");
    private readonly CheckBox _wholeLine = Check("Whole line");
    private readonly CheckBox _doubleBlanks = Check("Double blanks");

    /// <summary>Creates the header line controls.</summary>
    public ViewOptionsBar()
    {
        Dock = DockStyle.Top;
        AutoSize = true;
        WrapContents = true;
        Padding = new Padding(4);

        Controls.AddRange(
        [
            Caption("Pre. lines"), _preLines,
            Caption("Post. lines"), _postLines,
            _title, _lineNumbers, _fixedFont, _fileNames, _fileContents, _wholeLine, _doubleBlanks,
        ]);
    }

    /// <summary>Shows the given view options.</summary>
    public void LoadFrom(ResultViewOptions view)
    {
        _preLines.Value = Math.Clamp(view.ShowLinesBefore, 0, 99);
        _postLines.Value = Math.Clamp(view.ShowLinesAfter, 0, 99);
        _title.Checked = view.ShowTitle;
        _lineNumbers.Checked = view.ShowLineNumbers;
        _fixedFont.Checked = view.FixedFont;
        _fileNames.Checked = view.ShowFileNames;
        _fileContents.Checked = view.ShowFileContents;
        _wholeLine.Checked = view.ShowWholeLine;
        _doubleBlanks.Checked = view.ShowDoubleBlanks;
    }

    /// <summary>Writes the shown values into the given view options.</summary>
    public void SaveTo(ResultViewOptions view)
    {
        view.ShowLinesBefore = (int)_preLines.Value;
        view.ShowLinesAfter = (int)_postLines.Value;
        view.ShowTitle = _title.Checked;
        view.ShowLineNumbers = _lineNumbers.Checked;
        view.FixedFont = _fixedFont.Checked;
        view.ShowFileNames = _fileNames.Checked;
        view.ShowFileContents = _fileContents.Checked;
        view.ShowWholeLine = _wholeLine.Checked;
        view.ShowDoubleBlanks = _doubleBlanks.Checked;
    }

    private static NumericUpDown Number() => new() { Minimum = 0, Maximum = 99, Width = 50, Margin = new Padding(3, 3, 12, 3) };

    private static CheckBox Check(string text) => new() { Text = text, AutoSize = true };

    private static Label Caption(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 7, 0, 3) };
}
