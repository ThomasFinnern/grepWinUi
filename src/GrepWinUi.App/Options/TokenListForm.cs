using GrepWinUi.Core.AppData.Config;
using RecentLists;

namespace GrepWinUi.App.Options;

/// <summary>
/// Dialog to edit a token list (Options → Search string token, …) in two lists:
/// <b>Standard entries</b> (used ones, newest first) on top and <b>Pre entries</b> (fixed ones: kept
/// and not moved when an entry is used) below. Buttons move the selected entry between the lists;
/// new additions always go to the top of the target list. The four list settings are shown bottom
/// right and are the same values as in Settings. After Exit: <see cref="Rows"/> and <see cref="Options"/>.
/// </summary>
internal sealed class TokenListForm : Form
{
    private readonly ListBox _standard = TokenList();
    private readonly ListBox _pre = TokenList();
    private readonly TextBox _newEntry = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly NumericUpDown _preFixed = Number(0);
    private readonly NumericUpDown _firstFixed = Number(0);
    private readonly NumericUpDown _maxCount = Number(1);
    private readonly NumericUpDown _maxFixed = Number(0);
    private readonly StringComparer _comparer;

    /// <summary>Creates the dialog.</summary>
    /// <param name="title">Window title, e.g. "Search string token".</param>
    /// <param name="rows">Current entries (fixed = pre entries); each group keeps its order.</param>
    /// <param name="comparer">Decides if two entries are the same.</param>
    /// <param name="options">Current list settings (shown and edited in the dialog).</param>
    public TokenListForm(string title, IEnumerable<PinnedRow<string>> rows, StringComparer comparer, TokenListOptions options)
    {
        _comparer = comparer;
        Text = title;
        Size = new Size(640, 560);
        MinimumSize = new Size(520, 460);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;

        Set(_preFixed, options.PreFixedCount);
        Set(_firstFixed, options.FirstFixedCount);
        Set(_maxCount, options.MaxCount);
        Set(_maxFixed, options.MaxFixedCount);
        foreach (PinnedRow<string> row in rows)
        {
            (row.IsFixed ? _pre : _standard).Items.Add(row.Item);
        }

        // Only one entry is selected at a time, so the buttons know which list they work on.
        _standard.SelectedIndexChanged += (_, _) => ClearOtherSelection(_standard, _pre);
        _pre.SelectedIndexChanged += (_, _) => ClearOtherSelection(_pre, _standard);

        Controls.Add(Body());
        Controls.Add(Buttons());
    }

    /// <summary>Edited rows after Exit (standard entries, then pre entries, each in list order), else null.</summary>
    public IReadOnlyList<PinnedRow<string>>? Rows { get; private set; }

    /// <summary>Edited list settings after Exit, else null.</summary>
    public TokenListOptions? Options { get; private set; }

    private Control Body()
    {
        Button add = SideButton("Add");
        Button toPre = SideButton("To pre ↓");
        Button toStandard = SideButton("To standard ↑");
        Button up = SideButton("Move up");
        Button down = SideButton("Move down");
        Button delete = SideButton("Delete selected");
        add.Click += (_, _) => AddEntry();
        toPre.Click += (_, _) => MoveBetween(_standard, _pre);
        toStandard.Click += (_, _) => MoveBetween(_pre, _standard);
        up.Click += (_, _) => MoveSelected(-1);
        down.Click += (_, _) => MoveSelected(+1);
        delete.Click += (_, _) => DeleteSelected();
        _newEntry.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddEntry();
                e.SuppressKeyPress = true;
            }
        };

        TableLayoutPanel newRow = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
        newRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        newRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        newRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        newRow.Controls.Add(new Label { Text = "New entry", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        newRow.Controls.Add(_newEntry, 1, 0);
        newRow.Controls.Add(add, 2, 0);

        FlowLayoutPanel betweenLists = new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        betweenLists.Controls.AddRange([toPre, toStandard]);

        TableLayoutPanel lists = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        lists.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        lists.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        lists.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        lists.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        lists.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        lists.Controls.Add(ListCaption("Standard entries (newest first, used ones move to the top)"), 0, 0);
        lists.Controls.Add(_standard, 0, 1);
        lists.Controls.Add(betweenLists, 0, 2);
        lists.Controls.Add(ListCaption("Pre entries (fixed, never moved when used)"), 0, 3);
        lists.Controls.Add(_pre, 0, 4);

        FlowLayoutPanel moveButtons = new()
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(0, 20, 0, 0),
        };
        moveButtons.Controls.AddRange([up, down, delete]);

        Panel side = new() { Dock = DockStyle.Right, Width = 230, Padding = new Padding(8, 0, 0, 0) };
        side.Controls.Add(SettingsBox());
        side.Controls.Add(moveButtons);

        Panel body = new() { Dock = DockStyle.Fill, Padding = new Padding(10) };
        body.Controls.Add(lists);
        body.Controls.Add(side);
        body.Controls.Add(newRow);
        return body;
    }

    /// <summary>The four list settings, bottom right (same values as in Settings).</summary>
    private GroupBox SettingsBox()
    {
        TableLayoutPanel table = new() { Dock = DockStyle.Fill, ColumnCount = 2 };
        AddRow(table, "Newest used shown first", _preFixed);
        AddRow(table, "Then pre entries", _firstFixed);
        AddRow(table, "Max standard", _maxCount);
        AddRow(table, "Max pre", _maxFixed);

        GroupBox box = new() { Text = "List settings", Dock = DockStyle.Bottom, Height = 150 };
        box.Controls.Add(table);
        return box;
    }

    private Control Buttons()
    {
        Button cancel = new() { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        Button exit = new() { Text = "Exit", AutoSize = true };
        exit.Click += (_, _) =>
        {
            Rows =
            [
                .. _standard.Items.Cast<string>().Select(item => new PinnedRow<string>(item, IsFixed: false)),
                .. _pre.Items.Cast<string>().Select(item => new PinnedRow<string>(item, IsFixed: true)),
            ];
            Options = new TokenListOptions
            {
                PreFixedCount = (int)_preFixed.Value,
                FirstFixedCount = (int)_firstFixed.Value,
                MaxCount = (int)_maxCount.Value,
                MaxFixedCount = (int)_maxFixed.Value,
            };
            DialogResult = DialogResult.OK;
        };
        CancelButton = cancel;

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(8),
        };
        buttons.Controls.AddRange([exit, cancel]);
        return buttons;
    }

    /// <summary>The list with the selected entry, or null.</summary>
    private ListBox? SelectedList() =>
        _standard.SelectedIndex >= 0 ? _standard : _pre.SelectedIndex >= 0 ? _pre : null;

    /// <summary>Adds the new entry on top of the standard entries; an existing equal entry is only selected.</summary>
    private void AddEntry()
    {
        string entry = _newEntry.Text;
        if (entry.Trim().Length == 0)
        {
            return;
        }

        if (!SelectExisting(_standard, entry) && !SelectExisting(_pre, entry))
        {
            _standard.Items.Insert(0, entry);
            _standard.SelectedIndex = 0;
        }
        _newEntry.Clear();
    }

    private bool SelectExisting(ListBox list, string entry)
    {
        int index = list.Items.Cast<string>().ToList().FindIndex(item => _comparer.Equals(item, entry));
        if (index >= 0)
        {
            list.SelectedIndex = index;
        }
        return index >= 0;
    }

    /// <summary>Moves the selected entry of <paramref name="from"/> to the top of <paramref name="to"/>.</summary>
    private static void MoveBetween(ListBox from, ListBox to)
    {
        if (from.SelectedItem is not string entry)
        {
            return;
        }

        from.Items.RemoveAt(from.SelectedIndex);
        to.Items.Insert(0, entry);
        to.SelectedIndex = 0;
        to.Focus();
    }

    private void DeleteSelected()
    {
        if (SelectedList() is not ListBox list)
        {
            return;
        }

        int index = list.SelectedIndex;
        list.Items.RemoveAt(index);
        if (list.Items.Count > 0)
        {
            list.SelectedIndex = Math.Min(index, list.Items.Count - 1);
        }
    }

    private void MoveSelected(int step)
    {
        if (SelectedList() is not ListBox list)
        {
            return;
        }

        int index = list.SelectedIndex;
        int target = index + step;
        if (target < 0 || target >= list.Items.Count)
        {
            return;
        }

        object item = list.Items[index];
        list.Items.RemoveAt(index);
        list.Items.Insert(target, item);
        list.SelectedIndex = target;
    }

    private static void ClearOtherSelection(ListBox selected, ListBox other)
    {
        if (selected.SelectedIndex >= 0)
        {
            other.ClearSelected();
        }
    }

    private static ListBox TokenList() => new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        HorizontalScrollbar = true,
    };

    private static Label ListCaption(string text) => new() { Text = text, AutoSize = true, Padding = new Padding(0, 4, 0, 2) };

    private static NumericUpDown Number(int minimum) => new() { Minimum = minimum, Maximum = 999, Width = 60 };

    private static void Set(NumericUpDown number, int value) =>
        number.Value = Math.Clamp(value, (int)number.Minimum, (int)number.Maximum);

    private static void AddRow(TableLayoutPanel table, string label, Control control)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left });
        table.Controls.Add(control);
    }

    private static Button SideButton(string text) => new() { Text = text, AutoSize = true, MinimumSize = new Size(110, 0) };
}
