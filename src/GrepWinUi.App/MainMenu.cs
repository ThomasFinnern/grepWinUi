using GrepWinUi.Core;

namespace GrepWinUi.App;

/// <summary>Builds the main menu bar (File, Search, View, Options, Tools, Windows, Help).</summary>
internal static class MainMenu
{
    /// <summary>Creates the menu bar. Items without a handler in <paramref name="actions"/> are marked with "*".</summary>
    public static MenuStrip Create(MainMenuActions actions)
    {
        ToolStripMenuItem windowsMenu = Menu("&Windows",
            Item("Cascade", onClick: () => actions.ArrangeWindows(MdiLayout.Cascade)),
            Item("Tile horizontal", onClick: () => actions.ArrangeWindows(MdiLayout.TileHorizontal)),
            Item("Tile vertical", onClick: () => actions.ArrangeWindows(MdiLayout.TileVertical)),
            Item("Arrange Icons", onClick: () => actions.ArrangeWindows(MdiLayout.ArrangeIcons)));

        ToolStripMenuItem helpMenu = Menu("&Help",
            Item("Contents"),
            Item("Index"),
            Item("Search"),
            Separator(),
            Item("Info..."),
            Item("RegEx Info"));
        helpMenu.Alignment = ToolStripItemAlignment.Right;

        MenuStrip menuStrip = new();
        menuStrip.Items.AddRange(new ToolStripItem[]
        {
            CreateFileMenu(actions),
            CreateSearchMenu(actions),
            CreateViewMenu(),
            CreateOptionsMenu(actions),
            CreateToolsMenu(),
            windowsMenu,
            helpMenu,
        });
        menuStrip.MdiWindowListItem = windowsMenu;
        return menuStrip;
    }

    private static ToolStripMenuItem CreateFileMenu(MainMenuActions actions) => Menu("&File",
        Item("New search query", Keys.Control | Keys.N, actions.NewSearch),
        Item("Open search query", Keys.Control | Keys.O),
        Item("Save search query", Keys.Alt | Keys.S),
        Item("Close search view"),
        Item("Close All searches"),
        Item("Close All but this search"),
        Separator(),
        Item("Open search result"),
        Item("Save search result"),
        Menu("Save search result as ...",
            Item("Save as text"),
            Item("Save file list html"),
            Item("Save token lines html"),
            Item("Save as pdf"),
            Item("Save as rtf")),
        Separator(),
        Menu("Last used search queries", EmptyListItem()),
        Menu("Last used search files", EmptyListItem()),
        Separator(),
        Item("Exit", onClick: actions.ExitApp));

    private static ToolStripMenuItem CreateSearchMenu(MainMenuActions actions) => Menu("&Search",
        Item("Search", Keys.Control | Keys.F, actions.EditSearch),
        Item("Restart search", Keys.Alt | Keys.R),
        Item("Search in Files"),
        Separator(),
        Item("Replace"),
        Separator(),
        Item("Select all", Keys.Control | Keys.A),
        Item("&Cancel Search"));

    private static ToolStripMenuItem CreateViewMenu() => Menu("&View",
        Item("All matches"),
        Item("Matches in selected file"),
        Item("Selected file"),
        Separator(),
        Item("Previous match"),
        Item("Next match"),
        Separator(),
        Item("Previous File"),
        Item("Next file"));

    private static ToolStripMenuItem CreateOptionsMenu(MainMenuActions actions) => Menu("&Options",
        Item("Settings", onClick: actions.OpenSettings),
        Item("Search string lists"),
        Item("Search files spec. list"),
        Item("Search folder list"),
        Item("Command line search parameters"));

    private static ToolStripMenuItem CreateToolsMenu() => Menu("&Tools",
        Item("Test Text Search"),
        Item("Test Regular Search"),
        Item("Reference: Regular search expressions"),
        Item("Copy result file/folder names 2 clipboard"),
        Item("Copy search ID 2 clipboard"),
        Item("Feed back on error"));

    /// <summary>Menu or submenu that only holds other items (no own function, so no "*").</summary>
    private static ToolStripMenuItem Menu(string text, params ToolStripItem[] items)
    {
        ToolStripMenuItem menu = new(text);
        menu.DropDownItems.AddRange(items);
        return menu;
    }

    /// <summary>Menu item; without <paramref name="onClick"/> it gets the "*" (no function yet).</summary>
    private static ToolStripMenuItem Item(string text, Keys shortcut = Keys.None, Action? onClick = null)
    {
        ToolStripMenuItem item = new(MenuText.Display(text, isImplemented: onClick is not null)) { ShortcutKeys = shortcut };
        if (onClick is not null)
        {
            item.Click += (_, _) => onClick();
        }
        return item;
    }

    /// <summary>Disabled placeholder so an empty "last used" list still shows its submenu arrow.</summary>
    private static ToolStripMenuItem EmptyListItem() => new("(empty)") { Enabled = false };

    private static ToolStripSeparator Separator() => new();
}

/// <summary>Handlers for implemented menu items.</summary>
/// <param name="OpenSettings">Options → Settings.</param>
/// <param name="ExitApp">File → Exit.</param>
/// <param name="NewSearch">File → New search query: opens a new result form and its search properties.</param>
/// <param name="EditSearch">Search → Search: search properties of the active result form.</param>
/// <param name="ArrangeWindows">Windows → Cascade / Tile / Arrange Icons.</param>
internal sealed record MainMenuActions(Action OpenSettings, Action ExitApp, Action NewSearch, Action EditSearch, Action<MdiLayout> ArrangeWindows);
