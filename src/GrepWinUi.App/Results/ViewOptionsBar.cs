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
    private ResultViewOptions? _view;
    private bool _loading;

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

        foreach (NumericUpDown number in new[] { _preLines, _postLines })
        {
            number.ValueChanged += (_, _) => WriteBack();
        }
        foreach (CheckBox check in new[] { _title, _lineNumbers, _fixedFont, _fileNames, _fileContents, _wholeLine, _doubleBlanks })
        {
            check.CheckedChanged += (_, _) => WriteBack();
        }
    }

    /// <summary>Shows <paramref name="view"/> and writes each change of the controls back into it.</summary>
    public void Bind(ResultViewOptions view)
    {
        _view = view;
        LoadFrom(view);
    }

    /// <summary>Shows the given view options. Pre/post lines can't exceed the kept lines.</summary>
    public void LoadFrom(ResultViewOptions view)
    {
        _loading = true;
        _preLines.Maximum = Math.Clamp(view.KeepLinesBefore, 0, 99);
        _postLines.Maximum = Math.Clamp(view.KeepLinesAfter, 0, 99);
        _preLines.Value = Math.Clamp(view.ShowLinesBefore, 0, (int)_preLines.Maximum);
        _postLines.Value = Math.Clamp(view.ShowLinesAfter, 0, (int)_postLines.Maximum);
        _title.Checked = view.ShowTitle;
        _lineNumbers.Checked = view.ShowLineNumbers;
        _fixedFont.Checked = view.FixedFont;
        _fileNames.Checked = view.ShowFileNames;
        _fileContents.Checked = view.ShowFileContents;
        _wholeLine.Checked = view.ShowWholeLine;
        _doubleBlanks.Checked = view.ShowDoubleBlanks;
        _loading = false;
    }

    /// <summary>Raised after a change of the controls was written back into the bound view options.</summary>
    public event EventHandler? ViewChanged;

    private void WriteBack()
    {
        if (!_loading && _view is not null)
        {
            SaveTo(_view);
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }
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
