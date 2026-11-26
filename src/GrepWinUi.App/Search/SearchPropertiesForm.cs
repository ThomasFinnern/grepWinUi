using GrepWinUi.Core;
using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;
using static GrepWinUi.App.Search.SearchFormControls;

namespace GrepWinUi.App.Search;

/// <summary>
/// Modal dialog "Search properties": search string, file names, folder name and their options,
/// lines before/after a match. Works on copies of the result form's query and view options;
/// <see cref="Query"/> and <see cref="ViewOptions"/> hold the edited values after Exit or Start search.
/// Contains more options than the old program (also its hidden ones and ideas for later).
/// </summary>
internal sealed class SearchPropertiesForm : Form
{
    /// <summary>Width of the main combo boxes before the dialog is resized.</summary>
    private const int StartComboWidth = 480;

    /// <summary>Space right of a main combo box: its margin plus the "..." button with margin.</summary>
    private const int ListButtonSpace = 44;

    private readonly FlowLayoutPanel _content;
    private readonly Label _title = TitleBar();
    private readonly FlowLayoutPanel _buttons;
    private readonly int _minContentWidth;
    private readonly SearchQuery _query;
    private readonly ResultViewOptions _view;

    // --- Search string ---
    private readonly ComboBox _searchString = TextCombo();
    private readonly CheckBox _wholeWordsOnly = NoEffect(Check("Whole words only"));
    private readonly CheckBox _matchCase = NoEffect(Check("Match case"));
    private readonly CheckBox _useRegex = NoEffect(Check("Regular expression"));
    private readonly CheckBox _stopAfterFirstMatch = NoEffect(Check("First match per file only"));
    private readonly CheckBox _filesWithNoMatch = NoEffect(Check("List files without match"));
    private readonly CheckBox _linesWithNoMatch = NoEffect(Check("Non matching lines"));
    private readonly CheckBox _searchInFoundFiles = NoEffect(Check("Only in files of current result"));
    private readonly CheckBox _completeLine = NoEffect(Check("Match whole line"));
    private readonly CheckBox _fileNamesOnly = NoEffect(Check("File names only (no lines)"));
    private readonly CheckBox _fixedWidth = NoEffect(Check("Fixed-width field"));
    private readonly NumericUpDown _fixedBegin = Number(9999);
    private readonly NumericUpDown _fixedSize = Number(9999);
    private readonly CheckBox _delimited = NoEffect(Check("Delimited field"));
    private readonly TextBox _delimiter = new() { Width = 30, MaxLength = 1 };
    private readonly NumericUpDown _delimitedField = Number(999);
    private readonly CheckBox _useDateTime = NoEffect(Check("Filter by file date"));
    private readonly CheckBox _notOlderThan = NoEffect(Check("Modified within last"));
    private readonly NumericUpDown _olderThanValue = Number(9999);
    private readonly ComboBox _olderThanUnit = new()
    {
        Width = 80,
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly CheckBox _dateRange = NoEffect(Check("Modified between"));
    private readonly DateTimePicker _rangeStart = DatePicker();
    private readonly DateTimePicker _rangeEnd = DatePicker();

    // --- File types ---
    private readonly ComboBox _fileSpec = TextCombo();
    private readonly CheckBox _regexFileNames = NoEffect(Check("File names as RegEx"));
    private readonly CheckBox _searchInZip = NoEffect(Check("Search inside ZIP archives"));
    private readonly CheckBox _skipBinaryFiles = NoEffect(Check("Skip binary files"));
    private readonly CheckBox _onlyBinaryFiles = NoEffect(Check("Only binary files"));
    private readonly CheckBox _skipByExtension = NoEffect(Check("Skip by extension"));
    private readonly ComboBox _skipExtensions = TextCombo(400);

    // --- Folders ---
    private readonly ComboBox _folders = TextCombo();
    private readonly CheckBox _recurseFolders = NoEffect(Check("Include subfolders", isChecked: true));
    private readonly CheckBox _regexFolderNames = NoEffect(Check("Folder names as RegEx"));
    private readonly RadioButton _regexPathAndFileName = NoEffect(Radio("path and file name"));
    private readonly RadioButton _regexPathName = NoEffect(Radio("path name"));
    private readonly CheckBox _baseNameRegex = NoEffect(Check("Base name as RegEx"));
    private readonly TextBox _baseNamePattern = new() { Width = 300 };
    private readonly CheckBox _countFilesFirst = NoEffect(Check("Count files first (progress in %)"));
    private readonly CheckBox _skipFolders = NoEffect(Check("Skip folders"));
    private readonly ComboBox _skipFolderNames = TextCombo(400);

    // --- Lines around a match ---
    private readonly NumericUpDown _showLinesBefore = Number();
    private readonly NumericUpDown _showLinesAfter = Number();
    private readonly NumericUpDown _keepLinesBefore = Number();
    private readonly NumericUpDown _keepLinesAfter = Number();

    /// <summary>Creates the dialog filled with copies of <paramref name="query"/> and <paramref name="view"/>.</summary>
    /// <param name="query">Query of the result form.</param>
    /// <param name="view">View options of the result form (line counts).</param>
    /// <param name="lists">Recent lists for the dropdowns.</param>
    /// <param name="config">Settings (order of the search string, file name and folder name tokens in the dropdowns).</param>
    public SearchPropertiesForm(SearchQuery query, ResultViewOptions view, SearchLists lists, AppConfig config)
    {
        _query = query.Copy();
        _view = view with { };

        Text = "Search properties";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;

        _olderThanUnit.Items.AddRange(Enum.GetValues<TimeUnit>().Cast<object>().ToArray());
        _buttons = Buttons();
        LimitShowToKeep(_showLinesBefore, _keepLinesBefore);
        LimitShowToKeep(_showLinesAfter, _keepLinesAfter);

        _content = Column(
            _title,
            Section(NoEffect(SectionTitle("Search string")), _searchString, SearchStringOptions(), expanded: true),
            Section(NoEffect(SectionTitle("File names")), _fileSpec, FileTypeOptions(), SearchQueryText.HasFileNameOptions(_query)),
            Section(NoEffect(SectionTitle("Folder name")), _folders, FolderOptions(), SearchQueryText.HasFolderOptions(_query)),
            _countFilesFirst,
            LineNumbers(),
            _buttons);
        _content.AutoSize = false;
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.Padding = new Padding(10);
        Controls.Add(_content);

        FillDropDown(_searchString, _query.SearchString, lists.SearchStringTokens(config), StringComparer.Ordinal);
        FillDropDown(_fileSpec, _query.FileNames, lists.FileNameTokens(config), StringComparer.OrdinalIgnoreCase);
        FillDropDown(_folders, _query.FolderName, lists.FolderNameTokens(config), StringComparer.OrdinalIgnoreCase);
        LoadFrom(_query, _view);

        Size preferred = _content.GetPreferredSize(Size.Empty);
        _minContentWidth = preferred.Width - _content.Padding.Horizontal;
        ClientSize = preferred;
        MinimumSize = new Size(400, 300);

        _content.ClientSizeChanged += (_, _) => FitWidth();
        FitWidth();
        Shown += (_, _) => _searchString.Focus();
    }

    /// <summary>Edited query after Exit or Start search.</summary>
    public SearchQuery Query => _query;

    /// <summary>Edited view options (line counts) after Exit or Start search.</summary>
    public ResultViewOptions ViewOptions => _view;

    /// <summary>True when the dialog was closed with Start search.</summary>
    public bool StartSearchRequested { get; private set; }

    /// <summary>Fills the controls.</summary>
    private void LoadFrom(SearchQuery q, ResultViewOptions v)
    {
        _wholeWordsOnly.Checked = q.WholeWordsOnly;
        _matchCase.Checked = q.MatchCase;
        _useRegex.Checked = q.RegularExpression;
        _stopAfterFirstMatch.Checked = q.FirstMatchPerFileOnly;
        _filesWithNoMatch.Checked = q.ListFilesWithoutMatch;
        _linesWithNoMatch.Checked = q.NonMatchingLines;
        _searchInFoundFiles.Checked = q.OnlyInFilesOfCurrentResult;
        _completeLine.Checked = q.MatchWholeLine;
        _fileNamesOnly.Checked = q.FileNamesOnly;
        _fixedWidth.Checked = q.FixedWidthField;
        SetNumber(_fixedBegin, q.FixedWidthStartColumn);
        SetNumber(_fixedSize, q.FixedWidthWidth);
        _delimited.Checked = q.DelimitedField;
        _delimiter.Text = q.DelimitedSeparator;
        SetNumber(_delimitedField, q.DelimitedFieldNumber);
        _useDateTime.Checked = q.FilterByFileDate;
        _notOlderThan.Checked = q.ModifiedWithinLast;
        SetNumber(_olderThanValue, q.ModifiedWithinValue);
        _olderThanUnit.SelectedItem = q.ModifiedWithinUnit;
        _dateRange.Checked = q.ModifiedBetween;
        _rangeStart.Value = q.ModifiedFrom ?? DateTime.Today;
        _rangeEnd.Value = q.ModifiedTo ?? DateTime.Today;

        _regexFileNames.Checked = q.FileNamesAsRegex;
        _searchInZip.Checked = q.SearchInsideZipArchives;
        _skipBinaryFiles.Checked = q.SkipBinaryFiles;
        _onlyBinaryFiles.Checked = q.OnlyBinaryFiles;
        _skipByExtension.Checked = q.SkipByExtension;
        _skipExtensions.Text = q.SkippedExtensions;

        _recurseFolders.Checked = q.IncludeSubfolders;
        _regexFolderNames.Checked = q.FolderNamesAsRegex;
        _regexPathAndFileName.Checked = q.RegularExpressionOn == RegexTarget.PathAndFileName;
        _regexPathName.Checked = q.RegularExpressionOn == RegexTarget.PathName;
        _baseNameRegex.Checked = q.BaseNameAsRegex;
        _baseNamePattern.Text = q.BaseNamePattern;
        _skipFolders.Checked = q.SkipFolders;
        _skipFolderNames.Text = q.SkippedFolders;
        _countFilesFirst.Checked = q.CountFilesFirst;

        SetNumber(_keepLinesBefore, v.KeepLinesBefore);
        SetNumber(_keepLinesAfter, v.KeepLinesAfter);
        SetNumber(_showLinesBefore, v.ShowLinesBefore);
        SetNumber(_showLinesAfter, v.ShowLinesAfter);
    }

    /// <summary>Writes the controls into the copies.</summary>
    private void SaveTo(SearchQuery q, ResultViewOptions v)
    {
        q.SearchString = _searchString.Text;
        q.WholeWordsOnly = _wholeWordsOnly.Checked;
        q.MatchCase = _matchCase.Checked;
        q.RegularExpression = _useRegex.Checked;
        q.FirstMatchPerFileOnly = _stopAfterFirstMatch.Checked;
        q.ListFilesWithoutMatch = _filesWithNoMatch.Checked;
        q.NonMatchingLines = _linesWithNoMatch.Checked;
        q.OnlyInFilesOfCurrentResult = _searchInFoundFiles.Checked;
        q.MatchWholeLine = _completeLine.Checked;
        q.FileNamesOnly = _fileNamesOnly.Checked;
        q.FixedWidthField = _fixedWidth.Checked;
        q.FixedWidthStartColumn = (int)_fixedBegin.Value;
        q.FixedWidthWidth = (int)_fixedSize.Value;
        q.DelimitedField = _delimited.Checked;
        q.DelimitedSeparator = _delimiter.Text;
        q.DelimitedFieldNumber = (int)_delimitedField.Value;
        q.FilterByFileDate = _useDateTime.Checked;
        q.ModifiedWithinLast = _notOlderThan.Checked;
        q.ModifiedWithinValue = (int)_olderThanValue.Value;
        q.ModifiedWithinUnit = _olderThanUnit.SelectedItem is TimeUnit unit ? unit : TimeUnit.Days;
        q.ModifiedBetween = _dateRange.Checked;
        if (_dateRange.Checked)
        {
            q.ModifiedFrom = _rangeStart.Value.Date;
            q.ModifiedTo = _rangeEnd.Value.Date;
        }

        q.FileNames = _fileSpec.Text;
        q.FileNamesAsRegex = _regexFileNames.Checked;
        q.SearchInsideZipArchives = _searchInZip.Checked;
        q.SkipBinaryFiles = _skipBinaryFiles.Checked;
        q.OnlyBinaryFiles = _onlyBinaryFiles.Checked;
        q.SkipByExtension = _skipByExtension.Checked;
        q.SkippedExtensions = _skipExtensions.Text;

        q.FolderName = _folders.Text;
        q.IncludeSubfolders = _recurseFolders.Checked;
        q.FolderNamesAsRegex = _regexFolderNames.Checked;
        q.RegularExpressionOn = _regexPathName.Checked ? RegexTarget.PathName : RegexTarget.PathAndFileName;
        q.BaseNameAsRegex = _baseNameRegex.Checked;
        q.BaseNamePattern = _baseNamePattern.Text;
        q.SkipFolders = _skipFolders.Checked;
        q.SkippedFolders = _skipFolderNames.Text;
        q.CountFilesFirst = _countFilesFirst.Checked;

        v.KeepLinesBefore = (int)_keepLinesBefore.Value;
        v.KeepLinesAfter = (int)_keepLinesAfter.Value;
        v.ShowLinesBefore = (int)_showLinesBefore.Value;
        v.ShowLinesAfter = (int)_showLinesAfter.Value;
        v.LimitShowToKeep();
    }

    /// <summary>Tab / Shift+Tab follow <see cref="TabOrder"/> instead of the container order.</summary>
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if ((keyData & (Keys.KeyCode | Keys.Control | Keys.Alt)) == Keys.Tab
            && MoveFocus(forward: (keyData & Keys.Shift) == 0))
        {
            return true;
        }
        return base.ProcessDialogKey(keyData);
    }

    /// <summary>
    /// Tab order: search string, file names, folder name first, then all other inputs in layout order
    /// (only visible and enabled ones).
    /// </summary>
    private List<Control> TabOrder() =>
        [.. FocusOrder.FirstThenRest(Inputs(_content), [_searchString, _fileSpec, _folders])
            .Where(control => control.CanSelect && control.TabStop)];

    private bool MoveFocus(bool forward)
    {
        List<Control> order = TabOrder();
        if (order.Count == 0)
        {
            return false;
        }

        Control? active = ActiveControl;
        int current = order.FindIndex(control => control == active || (active is not null && control.Contains(active)));
        Control next = order[FocusOrder.NextIndex(order.Count, current, forward)];
        next.Focus();
        if (next is ComboBox { DropDownStyle: ComboBoxStyle.DropDown } combo)
        {
            combo.SelectAll();
        }
        else if (next is TextBoxBase text)
        {
            text.SelectAll();
        }
        return true;
    }

    /// <summary>Input controls in layout order; layout panels are opened, inputs (incl. NumericUpDown) are not.</summary>
    private static IEnumerable<Control> Inputs(Control parent)
    {
        foreach (Control child in parent.Controls.Cast<Control>().OrderBy(control => control.TabIndex))
        {
            if (child is Panel)
            {
                foreach (Control input in Inputs(child))
                {
                    yield return input;
                }
            }
            else
            {
                yield return child;
            }
        }
    }

    /// <summary>Exit and Start search: keep the edited values and close.</summary>
    private void Accept(bool startSearch)
    {
        SaveTo(_query, _view);
        StartSearchRequested = startSearch;
        DialogResult = DialogResult.OK;
    }

    private static void FillDropDown(ComboBox combo, string current, IEnumerable<string> items, StringComparer comparer)
    {
        combo.Items.AddRange(SearchQueryText.DropDownItems(current, items, comparer).Cast<object>().ToArray());
        combo.Text = current;
    }

    private static void SetNumber(NumericUpDown number, int value) =>
        number.Value = Math.Clamp(value, (int)number.Minimum, (int)number.Maximum);

    /// <summary>The shown lines can't be more than the kept lines.</summary>
    private static void LimitShowToKeep(NumericUpDown show, NumericUpDown keep)
    {
        show.Maximum = keep.Value;
        keep.ValueChanged += (_, _) => show.Maximum = keep.Value;
    }

    /// <summary>Title bar, button row and main combo boxes take the full width of the dialog.</summary>
    private void FitWidth()
    {
        int width = Math.Max(_minContentWidth, _content.ClientSize.Width - _content.Padding.Horizontal);
        _title.Width = width - _title.Margin.Horizontal;
        _buttons.Width = width;
        foreach (ComboBox combo in new[] { _searchString, _fileSpec, _folders })
        {
            combo.Width = width - ListButtonSpace;
        }
    }

    /// <summary>Opening a section grows the dialog (up to the screen height) so its options are visible.</summary>
    private void GrowToFitContent()
    {
        int missing = _content.GetPreferredSize(Size.Empty).Height - _content.ClientSize.Height;
        if (missing <= 0)
        {
            return;
        }

        Rectangle screen = Screen.FromControl(this).WorkingArea;
        Height = Math.Min(Height + missing, screen.Height);
        if (Bottom > screen.Bottom)
        {
            Top = Math.Max(screen.Top, screen.Bottom - Height);
        }
    }

    private static Label TitleBar() => new()
    {
        Text = "Search properties",
        AutoSize = false,
        Size = new Size(StartComboWidth + ListButtonSpace - 6, 30),
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.DarkGreen,
        ForeColor = Color.White,
        Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 12, FontStyle.Bold),
        Margin = new Padding(3, 3, 3, 10),
    };

    /// <summary>Bold section caption (name of the main parameter of the section).</summary>
    private static Label SectionTitle(string text)
    {
        Label caption = Caption(text);
        caption.Font = new Font(SystemFonts.MessageBoxFont!, FontStyle.Bold);
        caption.Width = 110;
        caption.AutoSize = false;
        return caption;
    }

    /// <summary>Section caption with "[+]" toggle, combo box with "..." button, options shown while "[+]" is checked.</summary>
    private FlowLayoutPanel Section(Label caption, ComboBox combo, Control options, bool expanded = false)
    {
        CheckBox more = Check("[+]", expanded);
        options.Visible = expanded;
        more.CheckedChanged += (_, _) =>
        {
            options.Visible = more.Checked;
            if (more.Checked)
            {
                GrowToFitContent();
            }
        };

        FlowLayoutPanel section = Column(Row(caption, more), Row(combo, ListButton()), options);
        section.Margin = new Padding(0, 0, 0, 12);
        return section;
    }

    /// <summary>Search string options in four columns: three of check boxes, one with the special list types.</summary>
    private Control SearchStringOptions() => Columns(
        Column(_wholeWordsOnly, _matchCase, _useRegex),
        Column(_stopAfterFirstMatch, _filesWithNoMatch, _linesWithNoMatch),
        Column(_searchInFoundFiles, _completeLine, _fileNamesOnly),
        Column(
            _fixedWidth,
            DependentRow(_fixedWidth, NoEffect(Caption("Start column")), _fixedBegin, NoEffect(Caption("Width")), _fixedSize),
            _delimited,
            DependentRow(_delimited, NoEffect(Caption("Separator")), _delimiter, NoEffect(Caption("Field no.")), _delimitedField),
            _useDateTime,
            DependentRow(_useDateTime, Column(
                Row(_notOlderThan, _olderThanValue, _olderThanUnit),
                Row(_dateRange, _rangeStart, Caption("-"), _rangeEnd)))));

    /// <summary>File type options in three columns; the skip list line below spans all of them.</summary>
    private Control FileTypeOptions() => Column(
        Columns(Column(_regexFileNames, _searchInZip), Column(_skipBinaryFiles, _onlyBinaryFiles), Column(_skipByExtension)),
        DependentRow(_skipByExtension, _skipExtensions, ListButton()));

    /// <summary>Folder options; the RegEx kind line sits right below "RegEx folder names" over both columns.</summary>
    private Control FolderOptions()
    {
        TableLayoutPanel table = new() { AutoSize = true, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty };
        table.Controls.Add(_recurseFolders, 0, 0);
        table.Controls.Add(_baseNameRegex, 1, 0);
        table.Controls.Add(_regexFolderNames, 0, 1);
        table.Controls.Add(DependentRow(_baseNameRegex, _baseNamePattern), 1, 1);
        Control regexKind = DependentRow(_regexFolderNames, NoEffect(Caption("Regular expression on:")), _regexPathAndFileName, _regexPathName);
        table.Controls.Add(regexKind, 0, 2);
        table.SetColumnSpan(regexKind, 2);

        return Column(
            table,
            _skipFolders,
            DependentRow(_skipFolders, _skipFolderNames, ListButton()));
    }

    /// <summary>One row per direction: lines shown and lines kept (prepared) around a match.</summary>
    private Control LineNumbers()
    {
        TableLayoutPanel grid = new() { AutoSize = true, ColumnCount = 5, RowCount = 2, Margin = new Padding(0, 0, 0, 8) };
        AddLineRow(grid, 0, "Lines before match:", _showLinesBefore, _keepLinesBefore);
        AddLineRow(grid, 1, "Lines after match:", _showLinesAfter, _keepLinesAfter);
        return grid;
    }

    private static void AddLineRow(TableLayoutPanel grid, int row, string title, NumericUpDown show, NumericUpDown keep)
    {
        grid.Controls.Add(NoEffect(Caption(title)), 0, row);
        grid.Controls.Add(NoEffect(Caption("show")), 1, row);
        grid.Controls.Add(show, 2, row);
        grid.Controls.Add(NoEffect(Caption("keep")), 3, row);
        grid.Controls.Add(keep, 4, row);
    }

    /// <summary>
    /// Cancel (Esc) discards; Exit keeps the values; Start search (Enter) keeps them and asks for
    /// a search, which has no function yet ("*").
    /// </summary>
    private FlowLayoutPanel Buttons()
    {
        Button cancel = new() { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        Button startSearch = new() { Text = "*Start search", AutoSize = true };
        Button exit = new() { Text = "Exit", AutoSize = true };
        startSearch.Click += (_, _) => Accept(startSearch: true);
        exit.Click += (_, _) => Accept(startSearch: false);
        CancelButton = cancel;
        AcceptButton = startSearch;

        FlowLayoutPanel buttons = Row(exit, startSearch, cancel);
        buttons.FlowDirection = FlowDirection.RightToLeft;
        buttons.AutoSize = false;
        buttons.Size = new Size(StartComboWidth + ListButtonSpace, 32);
        return buttons;
    }

    /// <summary>Controls side by side in a table, each in its own column (top aligned).</summary>
    private static TableLayoutPanel Columns(params Control[] columns)
    {
        TableLayoutPanel table = new() { AutoSize = true, ColumnCount = columns.Length, RowCount = 1, Margin = Padding.Empty };
        for (int i = 0; i < columns.Length; i++)
        {
            table.Controls.Add(columns[i], i, 0);
        }
        return table;
    }

    private static DateTimePicker DatePicker() => new() { Width = 100, Format = DateTimePickerFormat.Short };
}
