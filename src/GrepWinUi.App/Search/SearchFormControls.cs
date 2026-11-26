namespace GrepWinUi.App.Search;

/// <summary>Small factory helpers for the controls of the search properties form.</summary>
internal static class SearchFormControls
{
    /// <summary>
    /// Marks a parameter name whose value has no effect yet: its text is struck through.
    /// Remove the call in the step that makes the parameter work.
    /// </summary>
    public static T NoEffect<T>(T control) where T : Control
    {
        control.Font = new Font(control.Font, control.Font.Style | FontStyle.Strikeout);
        return control;
    }

    /// <summary>Checkbox with auto size.</summary>
    public static CheckBox Check(string text, bool isChecked = false) =>
        new() { Text = text, AutoSize = true, Checked = isChecked, Margin = new Padding(3, 2, 12, 2) };

    /// <summary>Radio button with auto size.</summary>
    public static RadioButton Radio(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(3, 2, 12, 2) };

    /// <summary>Label with auto size, aligned to the middle of a text box.</summary>
    public static Label Caption(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };

    /// <summary>Number field for counts (line numbers, positions, …).</summary>
    public static NumericUpDown Number(int maximum = 99) =>
        new() { Minimum = 0, Maximum = maximum, Width = 55 };

    /// <summary>Editable combo box for a search string, file spec or folder text.</summary>
    public static ComboBox TextCombo(int width = 480) =>
        new() { Width = width, DropDownStyle = ComboBoxStyle.DropDown };

    /// <summary>"..." button next to a combo box; "*" until it opens its list editor.</summary>
    public static Button ListButton() =>
        new() { Text = "*...", Width = 32, Height = 23 };

    /// <summary>Controls side by side.</summary>
    public static FlowLayoutPanel Row(params Control[] controls) => Flow(FlowDirection.LeftToRight, controls);

    /// <summary>Controls on top of each other.</summary>
    public static FlowLayoutPanel Column(params Control[] controls) => Flow(FlowDirection.TopDown, controls);

    /// <summary>Indented row that is only visible while <paramref name="toggle"/> is checked.</summary>
    public static FlowLayoutPanel DependentRow(CheckBox toggle, params Control[] controls)
    {
        FlowLayoutPanel row = Row(controls);
        row.Margin = new Padding(20, 0, 0, 0);
        row.Visible = toggle.Checked;
        toggle.CheckedChanged += (_, _) => row.Visible = toggle.Checked;
        return row;
    }

    private static FlowLayoutPanel Flow(FlowDirection direction, Control[] controls)
    {
        FlowLayoutPanel panel = new()
        {
            FlowDirection = direction,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
        };
        panel.Controls.AddRange(controls);
        return panel;
    }
}
