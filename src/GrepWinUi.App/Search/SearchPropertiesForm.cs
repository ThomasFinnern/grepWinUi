using static GrepWinUi.App.Search.SearchFormControls;

namespace GrepWinUi.App.Search;

/// <summary>
/// Modal dialog "Search properties": search string, file types, folders and their options.
/// Layout only for now: no data is loaded or written back, Start search has no function.
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
        DataSource = new[] { "Seconds", "Minutes", "Hours", "Days", "Weeks", "Months", "Years" },
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

    /// <summary>Creates the dialog with all inputs (empty). The search string options start open.</summary>
    public SearchPropertiesForm()
    {
        Text = "Search properties";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ShowInTaskbar = false;

        _regexPathAndFileName.Checked = true;
        _buttons = Buttons();

        _content = Column(
            _title,
            Section(NoEffect(SectionTitle("Search string")), _searchString, SearchStringOptions(), expanded: true),
            Section(NoEffect(SectionTitle("File names")), _fileSpec, FileTypeOptions()),
            Section(NoEffect(SectionTitle("Folder name")), _folders, FolderOptions()),
            _countFilesFirst,
            LineNumbers(),
            _buttons);
        _content.AutoSize = false;
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.Padding = new Padding(10);
        Controls.Add(_content);

        Size preferred = _content.GetPreferredSize(Size.Empty);
        _minContentWidth = preferred.Width - _content.Padding.Horizontal;
        ClientSize = preferred;
        MinimumSize = new Size(400, 300);

        _content.ClientSizeChanged += (_, _) => FitWidth();
        FitWidth();
        Shown += (_, _) => _searchString.Focus();
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

    /// <summary>Cancel (Esc) and Exit close the dialog; Start search has no function yet ("*").</summary>
    private FlowLayoutPanel Buttons()
    {
        Button cancel = new() { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        Button startSearch = new() { Text = "*Start search", AutoSize = true };
        Button exit = new() { Text = "Exit", AutoSize = true, DialogResult = DialogResult.OK };
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
