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
- **Status bar:** shows the search of the active result window. While searching: **Title**
  "Searching '…'", **Info** the file being read (Options → Settings → While searching →
  "Show file names in status bar"), **Working** a running animation, **Folder** the last name of the
  folder being read (`e:\wamp64\www\api_6x` shows `api_6x`), **Files** files read / all files
  (`12 / ?` when all files are not known), **Time** counting up. After the search: "Search done" or
  "Search cancelled", the number of matches, files and searched files, and the search time.
  Switching to another result window shows its status.
- **Count files first (progress in %):** tick it in the search properties dialog (below the Folder name
  section) to find all files before searching. The status bar then shows "Counting files for
  '…'", and while searching Files shows `12 / 57` and Working the progress in %. Without it, Working
  shows a running animation. New searches start with the value from Options → Settings → While
  searching → "Count files first and show progress in %".
- **Settings:** stored per user in `%AppData%\GrepWinUi\Config.json` (no admin rights needed).
  Until you change a setting, the standard values from `Defaults\Config.json` next to the exe are used.
- **File → New search query (Ctrl+N):** opens a new search result window inside the main window.
  It has a header line with view options (copied from the settings) and two panes, the file list on
  top and the matching lines below (drag the splitter between them).
  Needs the Microsoft Edge WebView2 Runtime (built into Windows 11).
  Then the search properties dialog opens over the new window. A new window starts with the
  newest search from the list of past searches (or the standard values).
  The window title shows `[number] search string :: file names :: folder`.
  The header line's pre/post lines can't be more than the lines kept (set in the dialog).
- **Lines before / after a match:** in the search properties dialog, "keep" sets how many lines
  before / after each matching line the search keeps; "show" (same as **Pre. lines** / **Post. lines**
  in the window's header line) sets how many of them the matching lines pane shows, in grey. Changing
  Pre. / Post. lines shows the result again at once, without searching. Each line is shown once; groups
  of lines that don't follow each other get a small space between them. A bigger "keep" value takes
  effect with the next search. Saved results (`*.result.json`) include the kept lines.
- **Click on a file** in the file list: the matching lines pane scrolls to that file's lines, and the
  file's row stays marked (light blue).
- **View options** (header line of a result window, standard values in Settings → Search result view):
  change the matching lines pane at once, without searching again.
  **Title** shows the blue header, **Line numbers** the `000012:` numbers, **File names** the path above
  each file's lines, **File contents** the lines (off: only the list of paths). **Whole line** off shows
  only the found parts of a matching line, separated by ` … `. **Fixed font** shows the lines in
  Consolas instead of Verdana. **Double blanks** off reduces runs of spaces and tabs to one blank.
- **File → Open search query (Ctrl+O):** opens a saved search query file in a new result window,
  together with its search properties dialog.
- **File → Load search query:** loads a saved search query into the active result window
  (replaces its search; the lines before/after stay) and opens the search properties dialog.
  Without an active result window it works like Open.
- **File → Save search query (Alt+S):** saves the search of the active result window to a file
  (`*.json`). Greyed out while no result window is active.
  The file dialogs start in the folder of the last saved, opened or loaded file (kept between program
  starts); at the very beginning in `%AppData%\GrepWinUi`. Search results will use the same folder.
- **Search (basic version):** **Start search** searches the folder from "Folder name" (with its
  subfolders when "Include subfolders" is ticked) for files matching "File names" (wildcards like
  `*.cs *.md`) and finds the search string in their lines. The window
  stays usable while searching. The file list pane shows a blue header with the search string and a
  summary, then a table of the files (file name, folder, number of matches). The matching lines pane
  shows each file's path, then its lines (`000012: text`, hits in green). Problems (missing folder,
  unreadable file) are listed too. The other search options follow step by step (their names are
  still struck through).
- **File and folder filters** (search properties dialog, `[+]` of File names and Folder name):
  - **Several folders:** separate them with blanks in "Folder name"; put a folder with blanks in
    double quotes (`"d:\My Folder" e:\src`). A single folder with blanks also works without quotes.
    Each file is searched once, also when folders overlap; a missing folder is listed as a problem.
  - **Include subfolders:** search the subfolders too.
  - **File names as RegEx:** "File names" is one regular expression on the file name (upper / lower
    case ignored, a part of the name is enough), e.g. `\.(cs|md)$`.
  - **Skip by extension:** e.g. `*.exe .dll obj` skips files with these extensions.
  - **Skip folders:** e.g. `.git bin obj*` (wildcards allowed); these subfolders and everything below
    them are not searched.
  - **Folder names as RegEx:** a regular expression on the path **below** the start folder, either
    "path and file name" (`src\tests\a.cs`) or "path name" (`src\tests`, empty for files in the start
    folder). Example: `\\tests$` with "path name" searches only folders named `tests`.
  - **Base name as RegEx:** only files whose own folder's last name matches are searched, e.g.
    `^(src|tests)$`; all subfolders are still looked into.
  - Start search checks every regular expression that is switched on and shows what is wrong.
- **Search string options** (search properties dialog, Search string section):
  - **Match case:** upper and lower case must match; without it case is ignored.
  - **Whole words only:** a hit must not have a letter, digit or `_` directly before or after it
    (also works for `.NET` or `C#`).
  - **Regular expression:** the search string is a .NET regular expression, e.g.
    `(public|private) function` or `^$` for empty lines. Without it, characters like `.` `*` `(` are
    searched as they are. Start search checks the expression first and shows what is wrong; a line
    that takes the expression longer than 2 s skips its file (listed as a problem).
  - **Match whole line:** the search string must cover the whole line; blanks at the start and end of
    the line don't count (an indented `    return x;` matches `return x;`).
- **Filter by file date** (search properties dialog, Search string section, 4th column): only files
  whose last modified time fits are searched. **Modified within last** N seconds … years (counted from
  the start of the search), **Modified between** two days (both days included); both ticked: both must
  fit. Start search warns when the start day is after the end day.
- **Text encodings:** each file is read with its own encoding, found from its first 8 KB: byte order
  mark (UTF-8, UTF-16, UTF-32), UTF-16 without byte order mark, UTF-8, else the ANSI code page of the PC
  (e.g. Windows-1252), so umlauts show right in all of them.
- **Binary files** (search properties dialog, File names section): a file with a 0 byte in its first
  8 KB counts as binary. **Skip binary files** leaves them out, **Only binary files** searches only them;
  without both they are searched too. In binary files control characters are shown as `·`.
  Very long lines (binary or minified files) are cut to 2000 characters around the first hit (`…` marks
  a cut end).
- **Result modes** (search properties dialog, Search string section):
  - **First match per file only:** each file shows only its first matching line (plus its lines after).
  - **List files without match:** lists only the files that do **not** contain the search string.
  - **Non matching lines:** shows the lines that do **not** contain the search string; the file list
    then counts lines ("Lines" column).
  - **File names only (no lines):** lists the files with a match, without lines (fast: each file is
    read only up to its first match).
  - The file list header and the status bar say what was found, e.g. "12 files without match from 57
    files searched".
- **Result colours and fonts (CSS):** each pane has its own style sheet, `FileList.css` and
  `MatchingLines.css`. The standard files are in `Defaults\` next to the exe; to change the look,
  copy one to `%AppData%\GrepWinUi\` and edit it there. Your copy is used from the next search on.
- **Search → Cancel Search:** stops the running search of the active result window. The panes show
  what was found until then, marked "cancelled". Greyed out while no search runs.
- **File → Save search result:** saves the result of the active window to a `*.result.json` file:
  the search parameters, the file list and the matching lines (with line numbers and hit positions),
  plus the counts and problems. Greyed out while the active window has no result.
- **File → Last used search files:** the search query and search result files you saved, opened or
  loaded last (newest on top, up to 15). A click opens the file: a result shows its saved lists, a query
  opens with its search properties dialog. A file that no longer exists is removed from the list.
- **Same search already open:** opening a query or result file (File → Open …, Last used search files)
  whose search (search string, file names, folder) is already shown in a window uses that window
  instead of a new one. A query keeps the shown result until you search again.
- **File → Open search result:** opens a saved `*.result.json` file in a new result window and shows
  its lists without searching again. Both use the same start folder as the search query files.
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
  **Start search** (Enter) also adds the whole search to the list of past searches and searches;
  **Cancel** (Esc) discards the changes. It already holds more options than the old program
  (file date filter, skip by extension, skip folders, fixed-width and delimited fields, …), to be reviewed.
  Parameter names that have no effect yet are struck through.
- **Windows → Cascade / Tile horizontal / Tile vertical / Arrange Icons:** arranges the result windows.
- **File → Exit:** closes the program.
- **Options → Settings:** edit the settings on six pages (Search result view, While searching,
  On start of program, Search string token, File name token, Folder name token). **Save** writes
  `Config.json`, **Cancel** discards all changes. New result forms start with a copy of these settings.
  The three token pages set their dropdown: how many newest standard entries come first, how many
  pre (fixed) entries follow, and how many standard / pre entries are kept
  (start values 7 / 3 / 30 / 15, for folders 7 / 3 / 40 / 15).
- **Options → Search string token:** edit the search strings of the dropdown in two lists.
  **Standard entries** (top): newest first; a used entry moves to the top, the oldest drop out.
  **Pre entries** (below): fixed, they are kept and never move when a search string is used.
  **To pre ↓** / **To standard ↑** move the selected entry to the top of the other list;
  **Add** puts a new entry on top of the standard entries. **Move up** / **Move down**,
  **Delete selected**. Bottom right: the four list settings, the same values as on the Settings page.
  **Exit** saves lists and settings, **Cancel** discards.
  The dropdown shows the newest standard entries first, then the pre entries, then the rest.
- **Options → File name token:** the same for the file name patterns of the file names dropdown
  (e.g. `*.cs *.md`); upper and lower case count as the same.
- **Options → Folder name token:** the same for the folders of the folder name dropdown;
  upper and lower case count as the same.

## Build and run

Needs the free .NET SDK 10 or newer (no Visual Studio needed). From the repository folder:

```
dotnet build GrepWinUi.slnx
dotnet test GrepWinUi.slnx
dotnet run --project src/GrepWinUi.App
```

Or use the batch files in `build\`: `build_debug.bat`, `build_release.bat`, plus `_rebuild`, `_clean`
and `_detailed` variants. All of them call `build.bat [Debug|Release] [extra args]`. 







