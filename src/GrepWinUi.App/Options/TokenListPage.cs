using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.App.Options;

/// <summary>
/// Settings page of one token list (e.g. "Search string token"): the four limits of
/// <see cref="TokenListOptions"/>. One page per list, the list is chosen by <c>select</c>.
/// </summary>
internal sealed class TokenListPage : OptionsPage
{
    private readonly string _title;
    private readonly Func<AppConfig, TokenListOptions> _select;
    private readonly NumericUpDown _preFixed = Number(0);
    private readonly NumericUpDown _firstFixed = Number(0);
    private readonly NumericUpDown _maxCount = Number(1);
    private readonly NumericUpDown _maxFixed = Number(0);

    /// <summary>Creates the page.</summary>
    /// <param name="title">Page title, e.g. "Search string token".</param>
    /// <param name="select">Picks the options of this list from the config.</param>
    public TokenListPage(string title, Func<AppConfig, TokenListOptions> select)
    {
        _title = title;
        _select = select;

        TableLayoutPanel table = new() { AutoSize = true, ColumnCount = 2 };
        AddRow(table, "Newest used items shown first", _preFixed);
        AddRow(table, "Then fixed items", _firstFixed);
        AddRow(table, "Max used items", _maxCount);
        AddRow(table, "Max fixed items", _maxFixed);

        Label help = new()
        {
            AutoSize = true,
            MaximumSize = new Size(380, 0),
            Margin = new Padding(3, 12, 3, 3),
            Text = "Dropdown order: the newest used items, then the fixed items, then the other used "
                + "items, then the other fixed items. Fixed items are ticked in the Options menu list "
                + "and never move when an entry is used.",
        };
        Controls.Add(Column(table, help));
    }

    /// <inheritdoc/>
    public override string Title => _title;

    /// <inheritdoc/>
    public override void LoadFrom(AppConfig config)
    {
        TokenListOptions options = _select(config);
        Set(_preFixed, options.PreFixedCount);
        Set(_firstFixed, options.FirstFixedCount);
        Set(_maxCount, options.MaxCount);
        Set(_maxFixed, options.MaxFixedCount);
    }

    /// <inheritdoc/>
    public override void SaveTo(AppConfig config)
    {
        TokenListOptions options = _select(config);
        options.PreFixedCount = (int)_preFixed.Value;
        options.FirstFixedCount = (int)_firstFixed.Value;
        options.MaxCount = (int)_maxCount.Value;
        options.MaxFixedCount = (int)_maxFixed.Value;
    }

    private static NumericUpDown Number(int minimum) => new() { Minimum = minimum, Maximum = 999, Width = 60 };

    private static void Set(NumericUpDown number, int value) =>
        number.Value = Math.Clamp(value, (int)number.Minimum, (int)number.Maximum);

    private static void AddRow(TableLayoutPanel table, string label, Control control)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left });
        table.Controls.Add(control);
    }
}
