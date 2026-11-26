# grepWinUi: developer documentation

How grepWinUi is built and why. Kept up to date with each step.

- Rules, technology, roadmap, conventions: [CLAUDE.md](../../CLAUDE.md)
- User-facing features: [README.md](../../README.md)
- Old program (netGrepWin, read only): [doc/netGrepWin/](../netGrepWin/) — `netGrepWin.code.md`,
  `searchForm.code.md`, `searchQuery.code.md` and the screenshots. Those files only describe the
  old program; decisions for grepWinUi are made here.
- Ideas not planned yet: [doc/ideas4later.md](../ideas4later.md)

## 1. Solution

| Project | Kind | Content |
|---|---|---|
| `src/GrepWinUi.App` | WinForms exe, `net10.0-windows` | Forms only: main window, menus, dialogs, WebView2 |
| `src/GrepWinUi.Core` | library, `net10.0` | All logic and data models (testable, no UI) |
| `src/lib_SaveAndLoad` | library, namespace `SaveAndLoad` | Save/load class objects as JSON |
| `src/lib_RecentList` | library, namespace `RecentLists` | Recent-first lists |
| `tests/*.Tests` | xUnit | One test project per library / Core |

References: App → Core → lib_RecentList → lib_SaveAndLoad.
No global "main" object: `Program.cs` creates the `ObjectStore` and loads `AppConfig`,
`MainForm` gets both and passes copies on.

## 2. Files on disk

| What | Where |
|---|---|
| User data (settings, lists, session) | `%AppData%\GrepWinUi\*.json` (written on first save) |
| Standard data files | `<exe>\Defaults\*.json` (read only, used when no user file exists) |
| WebView2 cache | `%LocalAppData%\GrepWinUi\WebView2` (one shared environment in `MainForm`) |

No admin rights needed: nothing is written next to the exe.

| Class | File |
|---|---|
| `AppConfig` | `Config.json` (Defaults file: `src/GrepWinUi.App/Defaults/Config.json`, a test keeps it equal to the built-in defaults) |
| `LastUsedFiles` | `LastUsed.json` |
| `SearchStringList` | `SearchStrings.json` |
| `FileNamesList` | `FileNames.json` |
| `FolderNameList` | `FolderNames.json` |
| `SearchQueryList` | `SearchQueries.json` |

## 3. Libraries

### lib_SaveAndLoad
- A stored class implements `IStorable` (`static string FileName`) and has only properties with defaults.
- Lists derive from `StoredList<TItem>` (`List<TItem> Items`).
- `ObjectStore(StorageFolders)`:
  - `Load<T>()`: user file → defaults file → `new T()`. Never throws; a problem text is in `LoadResult.Problem`.
  - `Save<T>()`: writes a temp file, then replaces the real one. Throws `IOException`.
- No UI: the App decides what to show.

### lib_RecentList
- `RecentList<TItem> : StoredList<TItem>`; derived classes add `IStorable` + `FileName`.
- `Add`: new item on top; an equal item (comparer from the constructor) moves to the top;
  items beyond `MaxCount` drop out.
- `MaxCount`: default 15, set in the derived constructor, changeable at runtime (shrinking trims),
  not stored in the file.

## 4. Core

### AppData/Config
- `AppConfig` (`Config.json`):
  - On program start: `OpenSearchFormOnStart`, `RepeatLastSearchOnStart`, `ReopenLastSearchesOnStart`,
    `ShowLastResultOnStart`.
  - While searching: `ShowFileNamesWhileSearching`, `ShowProgressInPercent`.
  - `ResultView` (`ResultViewOptions`).
  - `Copy()` (JSON round trip).
- `ResultViewOptions` (record): `KeepLinesBefore/After`, `ShowLinesBefore/After`, and the view
  checkboxes `ShowTitle`, `ShowLineNumbers`, `FixedFont`, `ShowFileNames`, `ShowFileContents`,
  `ShowWholeLine`, `ShowDoubleBlanks`.

### AppData/Session
- `LastUsedFiles` (`LastUsed.json`): last query / result / result HTML file and the newest one of
  them (`AnyFile`, used as fallback start folder of file dialogs).

### AppData/Search
- `SearchQuery`: all inputs of the search properties form.
  - Search string + options: `WholeWordsOnly`, `MatchCase`, `RegularExpression`,
    `FirstMatchPerFileOnly` (stops reading the current file), `ListFilesWithoutMatch`,
    `NonMatchingLines`, `OnlyInFilesOfCurrentResult`, `MatchWholeLine`, `FileNamesOnly`,
    fixed-width field (`StartColumn`, `Width`), delimited field (`Separator`, `FieldNumber`),
    file date filter (`ModifiedWithinLast` value + `TimeUnit`, `ModifiedBetween` from/to).
  - File names (space separated patterns, default `*.*`) + options: `FileNamesAsRegex`,
    `SearchInsideZipArchives`, `SkipBinaryFiles`, `OnlyBinaryFiles`, `SkipByExtension` + list.
  - Folder name + options: `IncludeSubfolders` (default on), `FolderNamesAsRegex` with
    `RegularExpressionOn` (`RegexTarget.PathAndFileName` / `PathName`), `BaseNameAsRegex` + pattern,
    `SkipFolders` + list.
  - `CountFilesFirst`.
  - Enums are stored as text in JSON.
  - `CreateDefault(config)`: defaults, `CountFilesFirst` from `config.ShowProgressInPercent`.
  - `Copy()`: independent copy (JSON round trip).
- `SearchQueryComparer`: **same search = same search string, file names and folder name.**
  Options are not compared. Search string with case, file/folder names ignore case.
- Recent lists: `SearchStringList` (35, case matters), `FileNamesList` (35, ignore case),
  `FolderNameList` (40, ignore case), `SearchQueryList` (15, `SearchQueryComparer`).
  Max counts as in the old program.
- `SearchLists`: the four lists together. `Load(store, out problems)` (never throws),
  `Save(store)` (throws `IOException` / `UnauthorizedAccessException`),
  `Remember(query, asPastQuery)` (non-empty search string / file names / folder on top of their
  lists; with `asPastQuery` also a copy of the whole query), `StartQuery(config)` (copy of the newest
  past query, else `SearchQuery.CreateDefault`).
- `SearchQueryText`: `Title(query)` ("search :: files :: folder", folder over 32 characters →
  "..." + last 30), `DropDownItems(current, items, comparer)` (current value first, no duplicates),
  `HasFileNameOptions` / `HasFolderOptions` (an option differs from its default → `[+]` starts open).
- `ResultViewOptions.LimitShowToKeep()`: shown lines ≤ kept lines.

### Other
- `AppInfo`: product name, main window title (`grepWinUi 1.2.3`).
- `MenuText.Display`: adds the `*` to menu items without a function.

## 5. App

### Program start
`Program.Main` loads `AppConfig` and the `SearchLists` (shows problem texts if any) and runs `MainForm`.
When `MainForm` is first shown and no result form is open, it opens a new empty result form
with the search properties dialog.

### MainForm (MDI container)
- `NewSearch()` (File → New search query, Ctrl+N): new `ResultForm` with `lists.StartQuery(config)`
  and a copy of `config.ResultView`, then the search properties dialog over it.
- `ShowSearchProperties(form)`: dialog with the form's `Query`, `ViewOptions` and the lists.
  On OK: `form.ApplySearch(...)`, `lists.Remember(...)`, `lists.Save(...)` (warning box on error).
- `EditSearch()` (Search → Search, Ctrl+F): dialog over the active result form; without one → `NewSearch()`.
- `OpenSettings()` (Options → Settings): `SettingsForm` on a copy; saves on OK.
- Windows menu arranges the MDI children; File → Exit closes.

### MainMenu
- Built in code from the screenshots (`*Menu.png`). Handlers come in `MainMenuActions`.
- Items without a handler get `*` automatically (`MenuText`).

### MainStatusBar
Fields Title, Info, Working (progress bar), Folder, Files, Time — dummy texts for now.
See "UI spec: status bar" in CLAUDE.md.

### Options/SettingsForm
Page list on the left (`OptionsPage` subclasses): Search result view, While searching, On start of program.
Works on `AppConfig.Copy()`; `Result` is set on Save.

### Results/ResultForm (MDI child)
- Header line `ViewOptionsBar` (pre/post lines + view checkboxes), file list pane on top,
  matching lines pane below (two WebView2 in a `SplitContainer`).
- Own `Query` and `ViewOptions` (copy of the settings). `ApplySearch(query, view)` takes over the
  dialog's values. Window title `[n] ` + `SearchQueryText.Title(Query)`.
- `ViewOptionsBar.Bind(view)`: shows the values and writes every change back into `view`
  (a `_loading` flag stops write-back while loading). Pre/post maximum = keep values.
- Shows `DummyResultHtml` for now.

### Search/SearchPropertiesForm (modal dialog)
- Opened with `ShowDialog(resultForm)`, centered over the result form.
- Constructor `(query, view, lists)`: works on copies; `LoadFrom` / `SaveTo` map every control to
  `SearchQuery` / `ResultViewOptions`. After OK: `Query`, `ViewOptions`, `StartSearchRequested`.
- Dropdowns: current value first, then the list items. Show ≤ keep (`Maximum` follows keep).
- Layout (built in code, `SearchFormControls` helpers):
  green title bar; sections **Search string**, **File names**, **Folder name**, each with a bold
  title, a `[+]` toggle for its options, a combo box and a `...` button; then
  "Count files first (progress in %)", the lines before/after a match (show / keep),
  and the buttons Cancel, Start search, Exit.
- Search string options: four columns (3 × 3 checkboxes + fixed-width / delimited / file date with
  their inputs, shown only when ticked). Always open at start; the other sections start open only
  when one of their options differs from its default.
- File names options: three columns, skip-by-extension list below.
- Folder options: two columns; the "Regular expression on:" line (path and file name / path name)
  directly below "Folder names as RegEx".
- Resizable: title bar, combo boxes and buttons follow the width; opening a section grows the
  dialog up to the screen height; scroll bars when smaller than the content.
- Cancel (Esc) discards; Exit and Start search (Enter) keep the values (Start search also sets
  `StartSearchRequested`; searching itself is missing, so it keeps its `*`). `...` has no function yet.
- **`NoEffect(...)`**: parameter names without effect are struck through; remove it in the step
  that makes the parameter work.

## 6. Behaviour decisions

- **Search form opening** (old program: the dialog always belongs to a result form):
  - Search → Search: active result form, else a new one.
  - File → New and program start without a result form: new empty result form with the dialog.
  - Planned: when the last forms are reopened on start, the dialog opens for the last active
    result form if Settings "Open search form" is set.
  - Planned (old program): Search in Files = Search with "Only in files of current result" ticked.
- **Dialog buttons** (as in the old program): Exit writes back and puts search string, file names
  and folder on top of their lists; Start search (Enter) does the same and also adds the whole query
  to the past queries (later: starts the search); Cancel (Esc) discards. Lists are saved after each OK.
- **New result form** starts with a copy of the newest past query (old program: last used query),
  else the defaults.
- **Skip binary files / Only binary files** stay independent checkboxes (decided in step 14).
- **Line counts** are not part of `SearchQuery`. They live in the result form's `ViewOptions`
  (copied from the settings when the form is created). The dialog's show/keep values and the
  header line's pre/post lines edit the same values. Shown lines can't exceed kept lines (as in the old program).
- **Count files first** starts with the Settings value and can be changed per search.
- **Same search** = same search string + file names + folder name: the user only sees an
  abbreviation in the list, so variants with other options can't be told apart; the newest
  options replace the older entry.
- **`[+]`**: search string section always open; file names and folder sections open when one of
  their options differs from its default (as in the old program).

## 7. Still to build (from the old program description)

Taken from `netGrepWin.code.md`; details per step in the roadmap.

- **File menu:** Open (select from last search queries), close this / all / all but this,
  open/save search results (search input + file list + token lines), each save adds to the
  last used files list; Last used search queries (click opens the query in a search form and
  starts the search); Last used search files (click opens the saved result without searching).
- **Search menu:** Cancel search (stop, keep what was found); Search in Files.
- **Options menu:** edit the recent lists (search strings, file names, folders); CSS colors.
- **MDI commands and keys** act on the active result form.
- **Lists** are saved on each change.
- **Result form:**
  - Each pane is HTML with its own CSS classes; each starts with a header, then a list,
    separated by a grey line.
  - File list: blue header with search string and file names; click on a file shows its lines
    in the lower pane and marks it as active; double click opens a "show and search inside file" form.
  - Matching lines: blue header "tokens found inside files"; the header line options decide what
    is shown (e.g. pre/post lines).
- **Old drawbacks to avoid:** UI freezes while searching (→ async), results lost after a popup
  question, admin rights for config (→ `%AppData%`).
