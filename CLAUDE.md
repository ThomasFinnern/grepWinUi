# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Code restart for a grep UI project grepWinUi with search forms 

This project will reimplement the functions in former project netGrepWin from scratch. 

## Repository state
- Solution `GrepWinUi.slnx` with `src/GrepWinUi.Core`, `src/GrepWinUi.App`, `src/lib_SaveAndLoad`
  and the test projects in `tests/` (see Architecture). Commands, run from the repo root:
  - Build: `dotnet build GrepWinUi.slnx`
  - Test: `dotnet test GrepWinUi.slnx`
  - Run: `dotnet run --project src/GrepWinUi.App`
  - Batch wrappers in `build\` (`build_debug.bat`, `build_release.bat`, …) all call `build\build.bat`.
  - A running grepWinUi window locks `bin\Debug`; close it before building.
- `doc/netGrepWin/` holds the description of the old program (`netGrepWin.code.md`)
  and UI screenshots (`*Menu.png`, `searchForm*.png`, `MdiFormWithResultsForm.png`).
  This is the main spec for the reimplementation.
- The old C# source (Visual Studio 2015) is in the sibling folder `..\netGrepWin`.
  Its central "god" object is `NetGrepWin\Program\MainGlobal\clsMainGlobal.cs`;
  config and command line definitions are in `NetGrepWin\Program`.
- Target design (from the old program): an MDI main window holding several search forms.
  Each search form shows HTML/CSS-styled results in two panes (file list and matching lines).
  Search queries and saved result files are kept in most-recent-first lists that are saved to disk.
  Config should be per user and must not need admin rights.

## Technology (decided 2026-11-25)
- **C# with WinForms on .NET 10 (LTS)**: free SDK, no Visual Studio license needed.
- Edit and debug in **VS Code** with the "C#" extension (`dotnet build`, `dotnet run`, F5).
- There is no WinForms designer in VS Code, so build forms in code
  (`Dock`, `TableLayoutPanel`, …) and don't use `*.Designer.cs` files.
- Use **WebView2** for the HTML/CSS result panes. The old project's `WebBrowser` control is based on IE and outdated.
  Its user data/cache folder is `%LocalAppData%\GrepWinUi\WebView2` (default next to the exe would need admin rights);
  one shared `CoreWebView2Environment` in `MainForm`. The App `.csproj` removes the package's unused WPF reference
  (it caused warning MSB3277).
- Run searches with `async/await` so the UI never freezes (a known problem of the old program).
- Store config in the user profile (`%AppData%`), never in the program folder, so no admin rights are needed.
- The new project does not reference the old project's code (.NET Framework 2.0 / 3.5 / 4.5);
  copy its logic over instead (search, regex, query lists).
  Development of the old project is finished: read its code, never change or run it.
- Target framework `net10.0-windows`. Any SDK ≥ 10 can build it; the dev PC has SDK 11.0.100
  plus the 10.0 and 11.0 WindowsDesktop runtimes. Also needed: the VS Code "C#" extension.

## Architecture (planned 2026-11-25)
```
GrepWinUi.slnx
src/GrepWinUi.Core/          class library (net10.0, no UI): search, query model,
                             recent-first lists, config, result files, HTML building
src/GrepWinUi.App/           WinForms (net10.0-windows): MDI window, forms, WebView2
src/lib_SaveAndLoad/         library (net10.0, namespace SaveAndLoad): save/load objects as JSON
src/lib_RecentList/          library (net10.0, namespace RecentLists): RecentList<TItem>, uses lib_SaveAndLoad
tests/GrepWinUi.Core.Tests/  xUnit tests for Core
tests/lib_SaveAndLoad.Tests/ xUnit tests for lib_SaveAndLoad (temp folders only)
tests/lib_RecentList.Tests/  xUnit tests for lib_RecentList
```
- All logic lives in Core so it can be tested; App only wires up forms.
- Tests: **xUnit**.
- Config: JSON (`System.Text.Json`) in `%AppData%\GrepWinUi\`. Standard data files in a
  `Defaults` folder next to the exe (one per stored class) are read when no user file exists;
  they are not copied, the user file is written on the first save. No group configs for now.
- Result files: own new JSON format; no reading of old `*.Res` files (import maybe later).
- UI: classic MDI child windows, as in the old program.
- Reusable libraries live in their own projects in folders starting with `lib_`
  (`src/lib_SaveAndLoad/`, `src/lib_CommandLine/`):
  - **`lib_SaveAndLoad`, read/write of class objects:** many classes are written to and read from disk
    (config, query lists, results, …). A `lib_` library does the reading and writing;
    each such class derives from / uses it and only defines its content items.
    Design (done): a class implements `IStorable` (declares `static string FileName`) and has
    only properties with default values; lists derive from `StoredList<TItem>`.
    `ObjectStore(StorageFolders)` does `Load<T>()` (user file → defaults file → `new T()`, never
    throws, problem text in `LoadResult`) and `Save<T>()` (temp file then replace; throws `IOException`).
    No UI in the library: the App decides what to show. No ties to the old LibConfig.
  - **`lib_RecentList`:** `RecentList<TItem> : StoredList<TItem>`. `Add` puts an item on top; an equal item
    (comparer from the constructor) moves to the top; oldest items beyond `MaxCount` drop out.
    `MaxCount` default 15, set in the derived constructor, changeable at runtime (shrinking trims),
    not stored in the file. Derived classes add `IStorable` + `FileName`.
  - **`lib_CommandLine`:** same approach. A `lib_` library does the parsing; the program only
    defines its command line items.
- Program-specific definition classes (only content items, no UI) live in Core under `AppData/`:
  - `AppData/Config/`: user settings (`AppConfig` → `Config.json`, with `ResultViewOptions`).
    Standard data file: `src/GrepWinUi.App/Defaults/Config.json` (copied to `<exe>\Defaults\`);
    a test checks it equals the built-in defaults, so keep both in sync.
  - `AppData/Session/`: state that changes often and must survive a restart
    (`LastUsedFiles` → `LastUsed.json`: last query/result/HTML file, fallback to the newest one).
  - `AppData/Search/`: `SearchQuery` (all inputs of the search form, no line counts; `Copy()`,
    `CreateDefault(config)`), `SearchQueryComparer` (same search = same search string,
    file names and folder name; options not compared, so newer options replace older ones) and the recent-first lists
    `SearchStringList`, `FileNamesList`, `FolderNameList` (30/30/40, file and folder names ignore case)
    and `SearchQueryList` (past queries).
  - `AppData/CommandLine/`: command line item definitions.
- A new result form starts with a **copy** of the current config settings
  (`config.ResultView with { }`), incl. the Keep lines (prepared lines before/after a match).
  The reusable mechanics stay in the `lib_` projects. No global "main" object: `Program.cs` / `MainForm`
  create the store and the config and pass them on.
- Before each feature step: read the matching code in `..\netGrepWin` (read only)
  and add short notes to `doc/netGrepWin/` (old program only).
- Developer documentation of grepWinUi: `doc/dev/grepWinUicode.md` (structure, classes, files on disk,
  behaviour decisions, what is still to build). Update it in every step that changes code or decisions.

## UI spec: status bar (main window)
Fields from left to right:
- **Title:** abbreviated info about the current task.
- **Info:** longer text about the current task or its result. Takes the remaining space.
- **Working:** progress bar (old name "Actual Info").
  - If enabled in config: shows in % how many files are done out of all files.
  - Otherwise: a running "up and down" animation (marquee) while an action runs.
- **Folder:** folder being searched, only its last name in the folder tree
  (e.g. `e:\wamp64\www\api_6x` shows `api_6x`).
- **Files:** count of files read so far / all files, e.g. `12 / 57`.
- **Time:** elapsed time, counting up while a search runs.

## Conventions
- Explicit types on public members, short XML doc comments (`///`).
- Keep functions small and pure where possible; no global state.
- Update README.md with each new feature, in the same step: a short user-facing description
  (what it does, where to find it: menu entry, key or command line option) under a "Features" section.
- Menu items without a working function get a `*` in front of their text (e.g. `*Open...`).
  Remove the `*` in the step that makes the function work.
- Parameter names in forms (checkboxes, radio buttons, input labels) whose value has no effect yet are
  struck through with `NoEffect(...)` (`SearchFormControls`). Remove `NoEffect` in the step that makes
  the parameter work, without being asked.

## How to work with me
- I build this in small steps. Do ONE step at a time, then stop.
- Before changing multiple files or the structure, explain the plan briefly and wait.
- Don't add features I didn't ask for; list ideas at the end instead.
- Add or update a test for each new behavior.

## planning:
My ideas but roadmap may be different and better 
1. First you need to scan for a function overview of the old project save it  
1. Then general planning with update of this document, this section and roadmap
1. Second we want to check which code language it suits best and how i can compile 
   it without Visual Studio license. if needed i may find an old C# 2010 version
1. Then we want to start with the outer form just all the menu items without functionality
1. Then we start with configuration for all user/?group active
1. Naked search form with search input and start form with elements. 
1. Save / load search input lists in several positions

Then further planning 

## old netGrepWin exe doc files

See file doc\netGrepWin\netGrepWin.code.md and the other files in folder doc\netGrepWin

### Issues

* State: Most functions finished, not all features are programmed
* Config code tries to write into windows admin restricted area and 
  therefore needs admin rights to start


## Tasks

* Grasp functionality of old project and save to file there for further read. Update this file.
* 
* 

## Running the old program for tests

- When coded: A rerun of the exe starts with the last search window(s) open again (`ReopenFiles.txt`,
  `SearchResult.Res` in `%AppData%\NetGrepWin\Config`; the result file can be large, so the main window
  may take 10+ s to appear).
- Before testing menus: use **Exit** on that reopened search form. Then one form, the last search result
  form, is active in the MDI window, and all menus and functions can be used.


## Roadmap
One step = one session. Steps 1–16c (with 13a, 13b, 16b) are done.
1. [x] Choose language/toolchain: C# WinForms on .NET 10, VS Code (see Technology)
2. [x] Install .NET SDK (11.0.100 installed)
3. [x] Install VS Code "C#" extension
4. [x] Planning the reimplementation (see Architecture)
5. [x] Skeleton: solution, Core/App/Tests projects, empty window that starts, `.gitignore`;
       add build/run/test commands to this file
6. [x] Main MDI window: all menus from the screenshots, status bar layout with all fields
       from "UI spec: status bar" (placeholder texts), No "Ctrl Shift F6" toolbar; no functions
7. [x] `lib_SaveAndLoad` library for reading/writing class objects (classes only define their content items)
8. [x] Config: settings model using `lib_SaveAndLoad`, JSON in `%AppData%\GrepWinUi\`, standard data file
       in `Defaults`, read when no user file exists
9. [x] Create code for first config menu item now named settings instead
10. [x] Recent-first list (`lib_RecentList`): newest on top, duplicates move to top, size limit, saved to disk (tested)
11. [x] Search result child form (MDI child): layout of header line with view options and the two
       result panes (file list, matching lines) with dummy content; File → New opens one; no search yet
12. [x] Result form: header line with view options, two WebView2 panes; HTML/CSS built in Core: basics
13a. [x] Search properties form, layout only: all inputs from `searchForm.png` and the old
       `frmSearchProperties`; opened with Search → Search (Ctrl+F) as a modal dialog over the active
       result form (without one, a new result form is created first); File → New and program start
       without a result form: new empty result form with the dialog open; empty dropdowns, no data, no search
13b. [x] Search query model: a `SearchQuery` class in Core with exactly the fields of the form;
       recent-first lists (search strings, file specs, folders, past queries); tests
14. [x] Connect form and model: the form is filled with the active result form's search data
       and writes the changes back to it; dropdowns filled from the lists; no search.
       Line counts: the dialog's show/keep values and the header line's pre/post lines edit the same
       `ViewOptions` of the result form (copied from the settings when the form is created).
15. [x] File menu "Save search query" (Alt+S): write the active result form's query to a file
       (own JSON format). "Open search query" (Ctrl+O): load a query file; opening starts a new
       result (search) form with that query and its search properties dialog.
       New File menu item "Load search query" (below "Open search query"): load a query file and
       replace the query of the active result form (without one, a new result form is created);
       it opens the search properties dialog too. Files: `*.json` with a header; Save is greyed out
       without an active result form.
       File dialogs start in the folder of the last saved / opened / loaded file
       (`LastUsedFiles.LastUsedFile`, kept between starts; at the beginning `%AppData%\GrepWinUi`),
       the search results will use the same folder.
       Also: Tab order in the search form (search string → file names → folder name first);
       menu shortcuts (Ctrl+F, …) work inside the WebView2 panes.
16. [x] Options menu "Search string token" (was "Search string lists"): token list as in the old
       program = used items (newest first) + fixed items (never moved by a new / known entry);
       dropdown order from 4 parameters (pre fixed count, first fixed count, max count, max fixed count;
       start values 7 / 3 / 30 / 15) in a separate Settings page "Search string token"; edit dialog
       (ticked = fixed, always in dropdown order, Add, Delete, Move up/down, the 4 parameters bottom
       right shared with Settings, Exit, Cancel). Generic parts: `PinnedRecentList`, `TokenListPage`,
       `TokenListForm`.
16b. [x] Options menu "File name token" (was "Search files spec. list"): same as 16 for the file
       names list, own Settings page. Token dialog reworked for both lists: two lists
       "Standard entries" (used) on top and "Pre entries" (fixed) below, buttons "To pre ↓" /
       "To standard ↑" (new additions go to the top), no checkboxes
16c. [x] Options menu "Folder name token" (was "Search folder list"): same as 16 for the folder
       names list, own Settings page (start values 7 / 3 / 40 / 15)
17. [ ] Search engine (Core): file enumeration (wildcard/regex, subfolders), line matching
       (regex, whole word, match case, lines before/after); async, cancellable, progress
18. [ ] Result form: two WebView2 panes; HTML/CSS built in Core: fill with search data, style like in old project with classes
19. [ ] Connect search to results: start/cancel
20. [ ] Status bar Title and Info: short and long text about the current task / result
21. [ ] Status bar Working: % of files done (config option, needs the file count before searching),
       otherwise marquee animation while an action runs
22. [ ] Status bar Folder (last folder name), Files (read / all) and Time (counting while a search runs)
23. [ ] Result clicks: click on a file jumps to its lines; double click opens the "show file" form
24. [ ] Save/open search results and the recent result files list (File menu)
25. [ ] Reopen the last forms on start; the dialog then opens for the last active result form
       if "Open search form" is set in Settings (On start of program)
26. [ ] Replace
27. [ ] Search in found files, "search again" key
28. [ ] Options dialog: config editor, CSS colors
29. [ ] `lib_CommandLine` library for command line parsing (program only defines its items)
30. [ ] Command line
31. [ ] Options menu: "Command line search parameters"


Ideas for later (not planned): ZIP search, printing, Explorer right-click entry,
fixed-width/delimited field search, import of old `*.Res` files, group configs.


## Current step
Step 17: Search engine (Core): file enumeration (wildcard/regex, subfolders), line matching
(regex, whole word, match case, lines before/after); async, cancellable, progress.
