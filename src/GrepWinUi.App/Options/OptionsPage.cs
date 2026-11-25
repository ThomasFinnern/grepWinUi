using GrepWinUi.Core.AppData.Config;

namespace GrepWinUi.App.Options;

/// <summary>One page of the preferences dialog. Shows and edits a part of <see cref="AppConfig"/>.</summary>
internal abstract class OptionsPage : UserControl
{
    /// <summary>Title shown in the page list and on top of the page.</summary>
    public abstract string Title { get; }

    /// <summary>Fills the controls from the config.</summary>
    public abstract void LoadFrom(AppConfig config);

    /// <summary>Writes the control values into the config.</summary>
    public abstract void SaveTo(AppConfig config);

    /// <summary>Vertical list of controls, used as page body.</summary>
    protected static FlowLayoutPanel Column(params Control[] controls)
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };
        panel.Controls.AddRange(controls);
        return panel;
    }

    /// <summary>Checkbox with auto size.</summary>
    protected static CheckBox Check(string text) => new() { Text = text, AutoSize = true };
}
