using GrepWinUi.Core.AppData.Config;
using RecentLists;

namespace GrepWinUi.App.Options;

/// <summary>
/// Dialog to edit a token list (Options → Search string token, …): one row per entry, ticked = fixed.
/// The rows are always shown in dropdown order (newest used entries first, then the fixed ones, then
/// the rest); they are arranged again after each change. The four list settings are shown bottom right
/// and are the same values as in Settings. After Exit: <see cref="Rows"/> and <see cref="Options"/>.
/// </summary>
internal sealed class TokenListForm : Form
{
    private readonly CheckedListBox _list = new()
    {
        Dock = DockStyle.Fill,
        CheckOnClick = true,
        IntegralHeight = false,
        HorizontalScrollbar = true,
    };

    private readonly TextBox _newEntry = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly NumericUpDown _preFixed = Number(0);
    private readonly NumericUpDown _firstFixed = Number(0);
    private readonly NumericUpDown _maxCount = Number(1);
    private readonly NumericUpDown _maxFixed = Number(0);
    private readonly StringComparer _comparer;
    private bool _arranging;

    /// <summary>Creates the dialog.</summary>
    /// <param name="title">Window title, e.g. "Search string token".</param>
    /// <param name="rows">Current entries.</param>
    /// <param name="comparer">Decides if two entries are the same.</param>
    /// <param name="options">Current list settings (shown and edited in the dialog).</param>
    public TokenListForm(string title, IEnumerable<PinnedRow<string>> rows, StringComparer comparer, TokenListOptions options)
    {
        _comparer = comparer;
        Text = title;
        Size = new Size(640, 500);
        MinimumSize = new Size(520, 400);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;

        Set(_preFixed, options.PreFixedCount);
        Set(_firstFixed, options.FirstFixedCount);
        Set(_maxCount, options.MaxCount);
        Set(_maxFixed, options.MaxFixedCount);
        ShowRows(rows, selected: null);

        // ItemCheck comes before the new state is set, so arrange afterwards; not while refilling the list.
        _list.ItemCheck += (_, _) =>
        {
            if (!_arranging)
            {
                BeginInvoke(Rearrange);
            }
        };
        _preFixed.ValueChanged += (_, _) => Rearrange();
        _firstFixed.ValueChanged += (_, _) => Rearrange();

        Controls.Add(Body());
        Controls.Add(Buttons());
    }

    /// <summary>Edited rows in list order after Exit, else null.</summary>
    public IReadOnlyList<PinnedRow<string>>? Rows { get; private set; }

    /// <summary>Edited list settings after Exit, else null.</summary>
    public TokenListOptions? Options { get; private set; }

    private Control Body()
    {
        Button add = SideButton("Add");
        Button up = SideButton("Move up");
        Button down = SideButton("Move down");
        Button delete = SideButton("Delete selected");
        add.Click += (_, _) => AddEntry();
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

        Label caption = new()
        {
            Text = "Entries in dropdown order (ticked = fixed: kept and not moved when an entry is used)",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 4),
        };

        FlowLayoutPanel moveButtons = new()
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
        };
        moveButtons.Controls.AddRange([up, down, delete]);

        Panel side = new() { Dock = DockStyle.Right, Width = 230, Padding = new Padding(8, 0, 0, 0) };
        side.Controls.Add(SettingsBox());
        side.Controls.Add(moveButtons);

        Panel body = new() { Dock = DockStyle.Fill, Padding = new Padding(10) };
        body.Controls.Add(_list);
        body.Controls.Add(side);
        body.Controls.Add(caption);
        body.Controls.Add(newRow);
        return body;
    }

    /// <summary>The four list settings, bottom right (same values as in Settings).</summary>
    private GroupBox SettingsBox()
    {
        TableLayoutPanel table = new() { Dock = DockStyle.Fill, ColumnCount = 2 };
        AddRow(table, "Newest used shown first", _preFixed);
        AddRow(table, "Then fixed", _firstFixed);
        AddRow(table, "Max used", _maxCount);
        AddRow(table, "Max fixed", _maxFixed);

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
            Rows = CurrentRows();
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

    private List<PinnedRow<string>> CurrentRows() =>
        [.. _list.Items.Cast<string>().Select((item, index) => new PinnedRow<string>(item, _list.GetItemChecked(index)))];

    /// <summary>Puts the rows in dropdown order again; the selected entry stays selected.</summary>
    private void Rearrange()
    {
        if (_arranging)
        {
            return;
        }
        string? selected = _list.SelectedItem as string;
        IReadOnlyList<PinnedRow<string>> arranged = PinnedRecentList<string>.Arrange(
            CurrentRows(), (int)_preFixed.Value, (int)_firstFixed.Value, _comparer);
        ShowRows(arranged, selected);
    }

    private void ShowRows(IEnumerable<PinnedRow<string>> rows, string? selected)
    {
        _arranging = true;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (PinnedRow<string> row in rows)
        {
            _list.Items.Add(row.Item, row.IsFixed);
        }
        _list.EndUpdate();
        _arranging = false;
        if (selected is not null)
        {
            _list.SelectedIndex = _list.Items.IndexOf(selected);
        }
    }

    /// <summary>Adds the new entry as newest used one; an existing equal entry is only selected.</summary>
    private void AddEntry()
    {
        string entry = _newEntry.Text;
        if (entry.Trim().Length == 0)
        {
            return;
        }

        string? existing = _list.Items.Cast<string>().FirstOrDefault(item => _comparer.Equals(item, entry));
        if (existing is null)
        {
            List<PinnedRow<string>> rows = CurrentRows();
            rows.Insert(0, new PinnedRow<string>(entry, IsFixed: false));
            ShowRows(rows, entry);
            Rearrange();
        }
        else
        {
            _list.SelectedItem = existing;
        }
        _newEntry.Clear();
    }

    private void DeleteSelected()
    {
        int index = _list.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        _arranging = true;
        _list.Items.RemoveAt(index);
        _arranging = false;
        Rearrange();
        if (_list.Items.Count > 0)
        {
            _list.SelectedIndex = Math.Min(index, _list.Items.Count - 1);
        }
    }

    /// <summary>Moves the selected entry; the dropdown order then keeps it within its group (used or fixed).</summary>
    private void MoveSelected(int step)
    {
        List<PinnedRow<string>> rows = CurrentRows();
        int index = _list.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        // Swap with the next row of the same group, so the move is visible after arranging.
        int target = index + step;
        while (target >= 0 && target < rows.Count && rows[target].IsFixed != rows[index].IsFixed)
        {
            target += step;
        }
        if (target < 0 || target >= rows.Count)
        {
            return;
        }

        (rows[index], rows[target]) = (rows[target], rows[index]);
        ShowRows(rows, rows[target].Item);
        Rearrange();
    }

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
