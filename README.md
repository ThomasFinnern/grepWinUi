# grepWinUi
In ancient times existed **"Windows Grep"** by Huw Millington which i used a lot. 
The program was a tool for searching files for text strings with a marvelous way 
to display the search results in a colored HTML view for Windows XP. 

Then it was not supported any more and i started with my own version. This is a 
redesign from start with the help of AI

## Features

- **Main window:** an MDI window titled "grepWinUi" plus version, which will hold the search forms.
- **Menu bar:** File, Search, View, Options, Tools, Windows, Help (Help on the right).
  Items marked with `*` have no function yet.
- **Status bar:** Title, Info, Working (progress bar), Folder, Files, Time. Shows dummy texts for now.
- **Settings:** stored per user in `%AppData%\GrepWinUi\Config.json` (no admin rights needed).
  Until you change a setting, the standard values from `Defaults\Config.json` next to the exe are used.
- **File → New search query (Ctrl+N):** opens a new search result window inside the main window.
  For now it shows dummy content: a header line with view options (copied from the settings) and
  two panes, the file list on top and the matching lines below (drag the splitter between them).
  Needs the Microsoft Edge WebView2 Runtime (built into Windows 11).
  Then the search properties dialog opens over the new window. A new window starts with the
  newest search from the list of past searches (or the standard values).
  The window title shows `[number] search string :: file names :: folder`.
  The header line's pre/post lines can't be more than the lines kept (set in the dialog).
- **File → Open search query (Ctrl+O):** opens a saved search query file in a new result window,
  together with its search properties dialog.
- **File → Load search query:** loads a saved search query into the active result window
  (replaces its search; the lines before/after stay) and opens the search properties dialog.
  Without an active result window it works like Open.
- **File → Save search query (Alt+S):** saves the search of the active result window to a file
  (`*.json`). Greyed out while no result window is active.
  The file dialogs start in the folder of the last saved, opened or loaded file (kept between program
  starts); at the very beginning in `%AppData%\GrepWinUi`. Search results will use the same folder.
- **Program start:** without an open result window, a new empty one opens together with its
  search properties dialog.
- **Search → Search (Ctrl+F):** opens the **Search properties** dialog over the active result window
  (without one, a new result window is created first). Ctrl+F (and the other menu shortcuts) also
  work while the cursor is inside one of the result panes; that window becomes the active one. Three sections: Search string, File names,
  Folder name, each with a text box with dropdown; `[+]` shows more options for the section
  (the search string options are shown from the start, in four columns). The dialog can be
  resized; the text boxes grow with its width, and opening a section makes the dialog taller.
  **Tab** jumps from the search string to the file names and the folder name first, then through
  the other inputs (Shift+Tab goes back).
  At the bottom: lines before / after a match to show and to keep (shown ≤ kept); they are the same
  values as the pre/post lines in the window's header line.
  The dialog shows the search of its result window; the dropdowns list the last used search strings,
  file names and folders (saved in `%AppData%\GrepWinUi\`).
  **Exit** keeps the changes for the window and remembers the texts in the dropdown lists;
  **Start search** (Enter) also adds the whole search to the list of past searches (searching itself
  is not there yet); **Cancel** (Esc) discards the changes. It already holds more options than the old program
  (file date filter, skip by extension, skip folders, fixed-width and delimited fields, …), to be reviewed.
  Parameter names that have no effect yet are struck through.
- **Windows → Cascade / Tile horizontal / Tile vertical / Arrange Icons:** arranges the result windows.
- **File → Exit:** closes the program.
- **Options → Settings:** edit the settings on three pages (Search result view, While searching,
  On start of program). **Save** writes `Config.json`, **Cancel** discards all changes.
  New result forms start with a copy of these settings.

## Build and run

Needs the free .NET SDK 10 or newer (no Visual Studio needed). From the repository folder:

```
dotnet build GrepWinUi.slnx
dotnet test GrepWinUi.slnx
dotnet run --project src/GrepWinUi.App
```

Or use the batch files in `build\`: `build_debug.bat`, `build_release.bat`, plus `_rebuild`, `_clean`
and `_detailed` variants. All of them call `build.bat [Debug|Release] [extra args]`. 







