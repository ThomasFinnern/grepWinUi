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
| Result pane style sheets | `%AppData%\GrepWinUi\FileList.css`, `MatchingLines.css` (user copies, optional) → `<exe>\Defaults\*.css` → built-in text in `ResultCss` |
| WebView2 cache | `%LocalAppData%\GrepWinUi\WebView2` (one shared environment in `MainForm`) |

No admin rights needed: nothing is written next to the exe.

| Class | File |
|---|---|
| `AppConfig` | `Config.json` (Defaults file: `src/GrepWinUi.App/Defaults/Config.json`, a test keeps it equal to the built-in defaults) |
| `LastUsedFiles` | `LastUsed.json` |
| `SearchStringList` | `SearchStrings.json` (`Items` = used, `Fixed` = fixed tokens) |
| `FileNamesList` | `FileNames.json` (`Items` = used, `Fixed` = fixed tokens) |
| `FolderNameList` | `FolderNames.json` (`Items` = used, `Fixed` = fixed tokens) |
| `SearchQueryList` | `SearchQueries.json` |
| `SearchQuery` (File → Save search query) | any `*.json` the user picks; header `Kind` = "grepWinUi search query", `Version` = 1, then `Query` |
| `SearchResult` (File → Save search result) | `*.result.json` the user picks; header `Kind` = "grepWinUi search result", `Version` = 1, `Saved` (date), then `Result` (query, files, lines with hits, counts, problems) |

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
- `PinnedRecentList<TItem> : RecentList<TItem>` (token lists as in the old program): a second stored
  list `Fixed` (pinned items) with `MaxFixedCount` (runtime, not stored). `Add` only changes the used
  items; fixed items never move. `Merged(preFixedCount, firstFixedCount)`: dropdown order = first
  *pre* used, then up to *first* fixed, then the other used, then the other fixed, each once.
  Static `Arrange(rows, pre, first, comparer)` puts `PinnedRow<TItem>(Item, IsFixed)` rows in that
  order (used and fixed rows each keep their order); `Merged` uses it.
  `EditRows(pre, first)` / `ReplaceAll(rows)` for an edit dialog: rows in dropdown order, each item
  once (fixed if in `Fixed`); replacing keeps row order, first of duplicates, and applies the limits.
  Old files without `Fixed` load with an empty fixed list.

## 4. Core

### AppData/Config
- `AppConfig` (`Config.json`):
  - On program start: `OpenSearchFormOnStart`, `RepeatLastSearchOnStart`, `ReopenLastSearchesOnStart`,
    `ShowLastResultOnStart`.
  - While searching: `ShowFileNamesWhileSearching`, `ShowProgressInPercent`.
  - `ResultView` (`ResultViewOptions`).
  - Token lists: `SearchStringTokens`, `FileNameTokens`, `FolderNameTokens` (`TokenListOptions`;
    folders with `MaxCount` 40 as in the old program).
  - `Copy()` (JSON round trip).
- `TokenListOptions` (record): start values (first start of the exe) `PreFixedCount` 7,
  `FirstFixedCount` 3, `MaxCount` 30, `MaxFixedCount` 15 (old program: 7 / 10 / 25 / 15). Old config files without the
  section get the defaults.
- `ResultViewOptions` (record): `KeepLinesBefore/After`, `ShowLinesBefore/After`, and the view
  checkboxes `ShowTitle`, `ShowLineNumbers`, `FixedFont`, `ShowFileNames`, `ShowFileContents`,
  `ShowWholeLine`, `ShowDoubleBlanks`.

### AppData/Session
- `LastUsedFiles` (`LastUsed.json`): last query / result / result HTML file and the newest one of
  them (`LastUsedFile`: full path of the last saved / opened / loaded file of any kind).
  `FileDialogFolder(appFolder)`: start folder of all file dialogs = folder of `LastUsedFile`,
  at the beginning the app's user folder. `StartFolder(lastFile, fallback)`: folder of the last
  file if it exists, else the fallback. `OrLastUsedFile(file)`: the file if set, else `LastUsedFile`.

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
- Recent lists: `SearchStringList` (30 used + 15 fixed from the settings, case matters),
  `FileNamesList` (30 used + 15 fixed from the settings, ignore case),
  `FolderNameList` (40 used + 15 fixed from the settings, ignore case), `SearchQueryList` (15, `SearchQueryComparer`).
  Max counts as in the old program.
- `SearchLists`: the four lists together. `Load(store, out problems)` (never throws),
  `Save(store)` (throws `IOException` / `UnauthorizedAccessException`),
  `Remember(query, asPastQuery)` (non-empty search string / file names / folder on top of their
  lists; with `asPastQuery` also a copy of the whole query), `StartQuery(config)` (copy of the newest
  past query, else `SearchQuery.CreateDefault`).
  `ApplySettings(config)`: list limits from the settings (at start and after Settings → Save).
  `SearchStringTokens(config)` / `FileNameTokens(config)` / `FolderNameTokens(config)`: merged dropdown items.
- `SearchStringList`, `FileNamesList` and `FolderNameList` are `PinnedRecentList<string>` (token lists).
- `SearchQueryText`: `Title(query)` ("search :: files :: folder", folder over 32 characters →
  "..." + last 30), `DropDownItems(current, items, comparer)` (current value first, no duplicates),
  `HasFileNameOptions` / `HasFolderOptions` (an option differs from its default → `[+]` starts open).
- `ResultViewOptions.LimitShowToKeep()`: shown lines ≤ kept lines.
- `SearchQueryFile`: `Save(query, path)` (indented JSON with header, temp file then replace),
  `Load(path)` (throws `InvalidDataException` when the file is broken or no query file, e.g. another
  JSON file; `IOException` / `UnauthorizedAccessException` when unreadable). Extension `.json`.

### Search (namespace `GrepWinUi.Core.Search`, step 17: basic version)
- Result model (records): `SearchResult(Query, Files, FilesSearched, FoldersSearched, MatchCount,
  Duration, Cancelled, Problems)`, `FileResult(Path, Lines, ContextLines)`, `LineMatch(LineNumber, Text, Hits)`
  (line numbers from 1), `Hit(Start, Length)` (0-based, every hit of a line),
  `ContextLine(LineNumber, Text)` (step 22: non-matching line kept near a match; `ContextLines` null →
  empty list, so result files saved before step 22 still load), `KeepLines(Before, After)`,
  `SearchProgress(FilesSearched, FoldersSearched, FilesMatched, CurrentFile)`.
- `FileFinder`: `Find(query, cancel)` (step 23; `Find(folder, fileNames, includeSubfolders, cancel)` builds
  a query) — start folders from `FolderList.Split(query.FolderName)`, filters from `FileFilter.Create(query)`
  (created at once, so an invalid RegEx throws `ArgumentException` before iterating). File names = space
  separated Win32 wildcards (`FileSystemName.MatchesWin32Expression`, case ignored, `*.*` = all, empty =
  all). Each file once (one pass per folder, all patterns checked; a `HashSet` across overlapping start
  folders), files of a folder before its subfolders, both sorted by name. Missing / unreadable folders →
  `Problems`, the search goes on. `FoldersSearched` counts. Returned paths are full paths.
- `ResultMode` (step 24): `Of(query)` = FirstMatchOnly / NonMatchingLines / FilesWithoutMatch / FileNamesOnly;
  `NoLines`, `CountOf(file)` (hits, or lines with NonMatchingLines; `SearchResult.MatchCount` is their sum),
  `Counts(result)` ("12 matches in 3 files from 57 files searched", "… lines without match in …",
  "… files without match from …", "… files with matches from …"; used by `ResultPageHtml.Summary` and
  `SearchStatus.Done`). `SearchEngine.SearchFile(…, mode)`: found line = hit XOR NonMatchingLines (no hits
  stored then); FirstMatchOnly keeps the first found line, later found lines inside its "after" range
  become context lines, then stops reading (old program: stopped at once, no lines after);
  FilesWithoutMatch / FileNamesOnly stop at the first found line and give a `FileResult` without lines
  (FileNamesOnly like grep -l, user decision; FilesWithoutMatch wins when both are set).
  `ResultPageHtml`: column "Matches" / "Lines" / none, `LinesTitleKind(query)` ("Lines with" / "Lines
  without" / "Files without" / "Files with"), "Found no files / lines without match for …".
- `FileKind(IsBinary, Encoding)` (step 25): `Detect(first 8 KB)` in this order: byte order mark (UTF-8,
  UTF-32 LE/BE, UTF-16 LE/BE) → UTF-16 without BOM (≥ 70 % of the byte pairs have a 0 on one side, ≤ 10 %
  on the other) → a 0 byte = binary (Latin-1, each byte one char) → valid UTF-8 (strict decoder, a char
  cut at the end of the sample allowed) → `Ansi` = code page of `CultureInfo.CurrentCulture.TextInfo
  .ANSICodePage` (needs `CodePagesEncodingProvider`, registered when `Ansi` is first used; Latin-1 if not
  available). `ShowControlCharacters(line)`: control chars except tab (also 0x7F–0x9F) → '·', same length.
- `LongLines` (step 25): `MaxLength` 2000; `Cut(LineMatch)` keeps a window starting `KeepBeforeHit` (200)
  chars before the first hit, hits outside dropped, others moved, `…` at cut ends; `Cut(string)` for kept
  lines (first 2000 chars). Keeps the pages below WebView2's ~2 MB `NavigateToString` limit. A huge line
  is still read into memory once by `ReadLine`.
- `FileFilter.StartProblems(query)` (step 25): `RegexProblems` + both binary options set + (step 26)
  `FileDateFilter.Problem`; used by the dialog's Start search (message box "Search not started").
- `FileDateFilter` (step 26, new: the old program had only the controls): `Create(query, now)` only with
  `FilterByFileDate`; "Modified within last" → from `Subtract(now, N, unit)` (months / years as calendar
  units, `MinValue` before year 1), "Modified between" → from start day 00:00 to before the day after the
  end day; both → the later start and the end. `Accepts(modified)` on `File.GetLastWriteTime` (local time,
  read only when `IsActive`, in `FileFinder.FindInAll`). `FileFinder.Find(query, cancel, now)`: `now` is the
  search start (default `DateTime.Now`). `Problem(query)`: start day after end day.
- `FolderList.Split(text, folderExists)` (step 23): blanks separate, double quotes keep blanks (as the old
  `ExtractFolderListWithApostrophes`), duplicates (case ignored) dropped; a whole text with blanks that is
  an existing folder is one folder (old inputs without quotes keep working).
- `FileFilter` (step 23), all case-insensitive, RegEx = `IsMatch` (part is enough), `CultureInvariant`,
  timeout `LineMatcher.RegexTimeout`:
  - `Accepts(relativeFolder, fileName)`: file names (wildcards, or one RegEx with `FileNamesAsRegex`),
    not skipped by extension (`ExtensionPatterns`: `*.exe` / `.exe` / `exe` → `*.exe`), and with
    `FolderNamesAsRegex` the new `SearchQuery.FolderNamePattern` on the path below the start folder:
    `RegexTarget.PathName` = relative folder ("" in the start folder), `PathAndFileName` = relative folder
    + file name. The start folder's own path is never matched (user decision).
  - `SkipsFolder(subfolder)`: "Skip folders" words as wildcards on the last name; not entered at all.
  - `SearchesFilesIn(folder)`: "Base name as RegEx" on the folder's last name; all folders are still
    walked, only the files of non-matching folders are left out (user decision, also the start folder).
  - `RegexProblems(query)`: problems of all switched-on RegEx inputs ("Search string: …", "File names:
    …", "Folder names: …", "Base name: …"); the dialog's Start search shows them and stays open.
- `LineMatcher` (step 21): abstract, `Find(line)` = all hits left to right, not overlapping.
  `Create(query)` picks one of two kinds (decided in step 21: regex only when asked for):
  - `PlainText(text, matchCase, wholeWords)`: `IndexOf` with `Ordinal` / `OrdinalIgnoreCase`; whole
    words = no `IsWordChar` (letter, digit, '_') directly before / after the hit (a rejected place
    retries one character further).
  - `RegularExpression(pattern, matchCase, wholeWords)`: `Regex` with `CultureInvariant`
    (+ `IgnoreCase` without Match case), whole words = `(?<!\w)(?:pattern)(?!\w)` (works for `.NET`, `C#`,
    `a|b`; old program: `\b…\b` without group), timeout `RegexTimeout` (2 s) per line. `Regex.Matches`
    moves on after a match of length 0 (`^$` gives a hit of length 0; old program looped forever).
  - `RegexError(pattern)`: problem text or null (used by the search dialog's Start search).
  - Step 24, `wholeLine` ("Match whole line", user decision: blanks around the line ignored): plain =
    `WholeLineMatcher` (trimmed line equals the text, hit = trimmed part); regex =
    `(?<=^\s*)(?:pattern)(?=\s*$)`. Whole words is not used then.
  - Test `PlainText_SameHitsAsEscapedRegex` keeps both kinds consistent (`IsWordChar` is close to `\w`,
    not equal for marks / connector punctuation other than '_').
- `SearchEngine.RunAsync(query, progress, cancel)`: `Task.Run` over `Run(...)` with a copy of the query.
  Empty search string → no search, problem "Search string is empty"; invalid regular expression →
  no search, problem "Invalid regular expression: …"; a regex timeout skips the file (problem). Files are read line by line
  (`StreamReader`) with the encoding of `FileKind.DetectFile(path)` (step 25). Unreadable file → `Problems`.
  Step 25: `Run` detects each file first; "Skip binary files" / "Only binary files" leave files out (they
  still count in `FilesSearched`); `SearchFile(…, kind)` replaces control characters of binary lines
  (`FileKind.ShowControlCharacters`) before matching and cuts long lines (`LongLines.Cut`). Progress at most every 100 ms and
  once at the end. Cancel → result so far with `Cancelled = true`, no exception.
  Step 22: `Run` / `RunAsync` / `SearchFile` take `KeepLines keep` (default none; the result form passes
  its `ViewOptions.KeepLinesBefore/After`). While reading a file the last `keep.Before` lines wait in a
  queue; a match moves them to the file's `ContextLines`, then the next `keep.After` non-matching lines
  are stored. Each line once, only for files with a match (old program: state machine with dictionaries).
- `SearchResultFile`: `Save(result, path)` (indented JSON with header, temp file then replace),
  `Load(path)` (throws `InvalidDataException` for broken files, query files or other JSON),
  `WithExtension(path)` (adds `.result.json`, replaces a plain `.json`). The result records are
  stored as they are (`System.Text.Json` fills the positional records through their constructors).
- `ResultCss` (step 18): one style sheet per pane, `FileListFileName` = `FileList.css`,
  `MatchingLinesFileName` = `MatchingLines.css`. `Load(folders, fileName)` → `CssText(Text, Source, Problem)`:
  user file → defaults file → built-in (`BuiltIn(fileName)`, constants `FileList` / `MatchingLines`);
  never throws, a file that can't be read is skipped (problem text). The Defaults files
  `src/GrepWinUi.App/Defaults/*.css` must equal the built-in texts (test, line endings ignored).
  The App reads the CSS for every page, so a changed user copy shows with the next search.
- `ResultPageHtml` (step 18, replaces the interim `ResultListHtml`): pages with the CSS text inside
  `<style>`, after a rule `body { background: … }` (window colour from the App, no WinForms in Core),
  so the CSS can override it. CSS class names from the old program:
  - `FileList(result, css, background)`: `div.HeaderFoundFiles` with h1 "Results for searched string: '…'"
    and h2 `Summary(result)`; `p.Problem` per problem; table File name / Folder / Matches
    (classes `FileName`, `Folder`, `Matches` on `th` and `td`; every column is as small as its content and
    left aligned, also columns added later; the file name
    is in a `div` with `title` = full name, cut with "…" beyond `max-width: 30em`) or `p.NotFound` "Found no matches for '…'".
  - `MatchingLines(result, view, css, background)`: `div.HeaderFoundToken` h1 "Lines with '…'"; per file
    `div.FileNameTitle` (full path), per line `div.Line` = `span.LineNumber` ("000012: ", `LineNumber(n)`,
    six digits as in the old program) + text with hits in `span.Token` (`MarkHits(line)`); or `p.NotFound`.
    Step 22: the lines come from `ShownLines(file, before, after)` (`view.ShowLinesBefore/After`): per match
    up to `before` kept lines not shown yet, the match, up to `after` kept lines before the next match;
    kept lines as `div.Line.ContextLine` (no hits); `div.Gap` before a line that doesn't follow the previous
    shown line, only when before/after > 0 (old program: `<br>` after every match line, post lines not
    cut at the next match). `MaxLines` counts all shown lines. CSS `.ContextLine` (grey), `.Gap` (0.8em).
    Step 27, view options (`ResultViewOptions`, only this pane, as in the old program): `ShowTitle` (header),
    `ShowFileNames` (`FileNameTitle`), `ShowFileContents` (all lines; off → list of paths),
    `ShowLineNumbers`; one line by `LineHtml(shown, view)`. `ShowWholeLine` off → `HitsOnly(line)`: only
    the hits, parted by `HitSeparator` " … " (old program: no separator); a line without hits and
    kept lines stay whole. `FixedFont` (new, old program: unused) → `<body class="FixedFont">`, the CSS
    sets Consolas only in `.FixedFont .Line` (Verdana otherwise). `ShowDoubleBlanks` off (default; new,
    old program: unused) → `ReduceBlanks`: every run of spaces / tabs becomes one space, also the indent;
    the hits are moved to mark the same text (a hit that shrinks to nothing is dropped).
  - `Hint(text, css, background)`; `Summary(result)` ("In files '…': found n matches in n files from n
    files searched in folder '…' (0.4 s)", plus " — cancelled").
  - All text HTML encoded; at most `MaxLines` (5000) lines, because `NavigateToString` takes only about 2 MB.
  - Size / date columns of the old file list are not there yet (the result model has no size/date).
- `SearchStatus` (step 19): status bar texts of one result form, record `(Title, Info, Folder, Files,
  Time, IsRunning, Percent)`. `Idle` (all empty), `Running(query, progress, elapsed, showFileNames)`
  ("Searching '…'", current file as Info only if `showFileNames`, Folder = last name of the current
  file's folder or of the search folder, Files "12 / ?"; with a known total (step 20) "12 / 57" and
  `Percent` = `PercentOf(read, all)`; while counting "Counting files for '…'" and Files = counted / ?;
  before the first report the query's `CountFilesFirst` decides), `Done(result)` ("Search done" / "Search cancelled", Info
  "154 matches in 48 files from 57 files searched", Files "57 / 57", cancelled "57 / ?"),
  `Failed(query, message, elapsed)`. Helpers `LastFolderName(folder)` (`e:\a\api_6x\` → `api_6x`, a
  drive root stays) and `TimeText(elapsed)` (`00:00:07`, hours beyond 24).

### Other
- `AppInfo`: product name, main window title (`grepWinUi 1.2.3`).
- `MenuText.Display`: adds the `*` to menu items without a function.
- `FocusOrder`: tab order helpers (main inputs first).

## 5. App

### Program start
`Program.Main` loads `AppConfig`, the `SearchLists` (shows problem texts if any) and `LastUsedFiles`,
and runs `MainForm`.
When `MainForm` is first shown and no result form is open, it opens a new empty result form
with the search properties dialog.

### MainForm (MDI container)
- `NewSearch()` (File → New search query, Ctrl+N): `OpenResultForm(lists.StartQuery(config))`.
- `OpenResultForm(query)`: new `ResultForm` with the query and a copy of `config.ResultView`,
  then the search properties dialog over it.
- `OpenSearchQuery()` (File → Open search query, Ctrl+O): query file → `OpenResultForm(query)`.
- `LoadSearchQuery()` (File → Load search query): query file → `form.ApplySearch(query, form.ViewOptions)`
  on the active result form (line counts stay), then the dialog; without one like Open.
- `OpenSearchResult()` / `SaveSearchResult()` (File → Open / Save search result, in
  `MainForm.ResultFiles.cs`): Open loads a `*.result.json` into a new result form
  (`CreateResultForm(result.Query.Copy())` + `ShowResultAsync(result)`, no dialog, no search);
  Save writes the active form's `Result` (greyed out without one; `ResultForm.ResultChanged` updates
  the menu). Both remember the file with `LastUsedFiles.RememberResultFile` (shared start folder).
- `SaveSearchQuery()` (File → Save search query, Alt+S): active form's `Query` → file.
  Greyed out without an active result form (`UpdateMenuState` on `MdiChildActivate` and after a
  result form closes).
- File dialogs: filter `SearchQueryFile.DialogFilter`, start in `LastUsedFiles.FileDialogFolder(appFolder)`
  (folder of `LastUsedFile` = last saved / opened / loaded file of any kind, at the beginning
  `%AppData%\GrepWinUi` from `ObjectStore.Folders.UserFolder`, created if missing; the search results
  will use the same folder). After save/open/load: `RememberSearchQueryFile`
  and save `LastUsed.json` (errors ignored: only a start folder). A broken file → warning box.
- `ShowSearchProperties(form)`: dialog with the form's `Query`, `ViewOptions` and the lists.
  On OK: `form.ApplySearch(...)`, `lists.Remember(...)`, `lists.Save(...)` (warning box on error).
- `EditSearch()` (Search → Search, Ctrl+F): dialog over the active result form; without one → `NewSearch()`.
- `OpenSettings()` (Options → Settings): `SettingsForm` on a copy; saves on OK, then
  `lists.ApplySettings(Config)`.
- `EditSearchStringTokens()`, `EditFileNameTokens()`, `EditFolderNameTokens()` (Options → Search string /
  File name / Folder name token) all call `EditTokenList(title, list, comparer, getOptions, setOptions)`:
  `TokenListForm` with `list.EditRows(pre, first)` and the list's `TokenListOptions`; Exit → settings
  into `Config.json`, `ApplySettings`, `ReplaceAll(rows)`, save the lists.
- Windows menu arranges the MDI children; File → Exit closes.

### MainMenu
- Built in code from the screenshots (`*Menu.png`). Handlers come in `MainMenuActions`.
- File → "Load search query" is new (not in the old program).
- Items the main window must find get a name (`Named(...)`, e.g. `MainMenu.SaveSearchQueryName`).
- Items without a handler get `*` automatically (`MenuText`).

### MainStatusBar
Fields Title, Info, Working (progress bar), Folder, Files, Time (see "UI spec: status bar" in CLAUDE.md).
`ShowStatus(SearchStatus)` writes "Title: …" etc.; Working while `IsRunning`: `Percent` as a normal bar
when known, else a marquee; empty when not running.
`MainForm.UpdateStatusBar()` shows the status of the active result form (`SearchStatus.Idle` without
one); called on `MdiChildActivate`, after a form is closed and on the active form's `StatusChanged`.

### Options/SettingsForm
Page list on the left (`OptionsPage` subclasses): Search result view, While searching, On start of
program, Search string token, File name token, Folder name token. Works on `AppConfig.Copy()`;
`Result` is set on Save.
- `TokenListPage(title, select)`: the four `TokenListOptions` numbers of one list; reused for the
  file name and folder name token pages.

### Options/TokenListForm (token list dialog)
- `TokenListForm(title, rows, comparer, options)`: two `ListBox`es, **Standard entries** (used,
  newest first) on top and **Pre entries** (fixed) below, each about half the height. Only one entry
  is selected at a time (selecting in one list clears the other). **To pre ↓** / **To standard ↑**
  move the selected entry to the top of the other list; "New entry" + **Add** puts it on top of the
  standard entries (an existing equal entry is only selected). **Move up** / **Move down** /
  **Delete selected** work on the selected list. Bottom right: group "List settings" with the four
  `TokenListOptions` numbers. **Exit** sets `Rows` (standard rows, then pre rows, each in list order)
  and `Options`; **Cancel** (Esc) discards. Reused for file name and folder name tokens.
- Wording in the UI: "standard entries" = used items (`Items`), "pre entries" = fixed items (`Fixed`).
- `MainForm.EditTokenList(...)`: on Exit the options go into the list's section of the config
  (`Config.json` saved, same values as the Settings page), then `ApplySettings`, `ReplaceAll(rows)`,
  lists saved.

### Results/ResultForm (MDI child)
- Header line `ViewOptionsBar` (pre/post lines + view checkboxes), file list pane on top,
  matching lines pane below (two WebView2 in a `SplitContainer`).
- Own `Query` and `ViewOptions` (copy of the settings). `ApplySearch(query, view)` takes over the
  dialog's values. Window title `[n] ` + `SearchQueryText.Title(Query)`.
- `ViewOptionsBar.Bind(view)`: shows the values and writes every change back into `view`
  (a `_loading` flag stops write-back while loading). Pre/post maximum = keep values.
  `ViewChanged` after each write-back: `ResultForm.ShowMatchingLinesAgainAsync()` builds only the
  matching lines pane again from `Result` (no search; nothing while searching). `ApplySearch` does the
  same when the dialog changed the show values.
- `InitPanesAsync()` (on `Load`, kept as `_panesReady`): starts both WebView2 panes with the shared
  environment and shows a hint ("No search yet …"); a missing WebView2 runtime → message box.
- `Result` (last finished or loaded result, null while searching) and `ResultChanged`;
  `ShowResultAsync(result)` shows a saved result without searching.
- `RunSearchAsync(showFileNames)`: cancels a running search of this form, shows "Searching …", awaits
  `SearchEngine.RunAsync(Query, progress, …)` and shows `ResultPageHtml.FileList` / `MatchingLines` with the CSS
  from `ResultCss.Load(cssFolders, …)` (`cssFolders` = `ObjectStore.Folders`, passed in by `MainForm`).
  Never throws (an unexpected error is shown in the pane); a newer search wins; closing the form cancels
  the search. Called by `MainForm.ShowSearchProperties` after Start search (fire and forget).
- `Status` / `StatusChanged` (step 19): `SearchStatus` of this form. While searching it is updated on
  each progress report (`Progress<T>` created on the UI thread, at most every 100 ms) and by a 250 ms
  WinForms timer (time); at the end `SearchStatus.Done` / `Failed`; `ShowResultAsync` sets `Done(result)`.
  `showFileNames` comes from Settings "Show file names in status bar" at search start.
- `IsSearching` / `CancelSearch()` (step 18): Search → Cancel Search stops the running search; the engine
  returns what it found so far (`Cancelled = true`) and the panes show it, marked "cancelled".
  `ResultChanged` is raised at search start and end, so `MainForm.UpdateMenuState` enables
  Search → Cancel Search (`MainMenu.CancelSearchName`) only while the active form searches.
- **Keys in the WebView2 panes:** the WinForms WebView2 control only raises its own `KeyDown` for
  shortcut keys and doesn't pass them to the window, so the main menu never sees them (Ctrl+F would
  open the browser's find bar). `OnPaneKeyDown` looks the key up with `MainMenu.FindShortcut`
  (enabled items without `*` only, so e.g. Ctrl+A still selects text in the pane), marks it handled,
  and later (`BeginInvoke`, the item may open a modal dialog) activates this form and runs the item.

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
- Tab order: `ProcessDialogKey` handles Tab / Shift+Tab with `TabOrder()` =
  `FocusOrder.FirstThenRest(inputs in layout order, [search string, file names, folder name])`,
  only visible and enabled inputs (`FocusOrder` in Core, tested). The three combo boxes sit in
  different sections, so normal `TabIndex` values can't give this order.
- Cancel (Esc) discards; Exit and Start search (Enter) keep the values; Start search also sets
  `StartSearchRequested` and the main window then starts the search. `...` has no function yet.
- Working parameters (no strike-through): the three section titles (search string, file names,
  folder name) and "Include subfolders"; all others are still `NoEffect`.
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
- **Query files** (step 15): own JSON format with a header, extension `.json`. Open → new result form,
  Load → replaces the active form's query; both open the search properties dialog. Save is greyed
  out without an active result form (old program: did nothing). Opening a query file does not
  add it to the past queries; the last used files list (File → Last used search files) comes later.
- **Line counts** are not part of `SearchQuery`. They live in the result form's `ViewOptions`
  (copied from the settings when the form is created). The dialog's show/keep values and the
  header line's pre/post lines edit the same values. Shown lines can't exceed kept lines (as in the old program).
  Keep values act on the next search; show values only change the view (step 22).
- **Count files first** starts with the Settings value and can be changed per search.
  Step 20 (new, the old program had only an unused checkbox): `SearchEngine` first collects all found
  files in a list (`CountFiles`, progress with `Counting = true` and the files counted so far in
  `TotalFiles`), then searches that list (no second folder walk) with `TotalFiles` = all files.
  Without it one single pass, `TotalFiles` null. A cancelled search shows Files "n / ?" at the end
  (the result has no total; would need a field in `*.result.json`).
- **Same search** = same search string + file names + folder name: the user only sees an
  abbreviation in the list, so variants with other options can't be told apart; the newest
  options replace the older entry.
- **`[+]`**: search string section always open; file names and folder sections open when one of
  their options differs from its default (as in the old program).
- **Search engine** (step 17): runs on a background thread (old program: UI thread, window froze);
  a file matching several patterns is searched once (old: once per pattern); inaccessible folders
  and unreadable files are noted and skipped (old: exception / message box); empty search string →
  no search (old: matched every line); encoding per file (step 25; old: ANSI code page for all files,
  BOM only); binary files detected (old: read like text).
- **Status bar** (step 19): one status bar in the main window showing the active result form's status
  (old: one per result form). Folder = last name of the folder being read (old: folder count);
  Files = read / all, "?" for all until files are counted first (step 20); Time = `hh:mm:ss`
  (old: `s7.123` at the end only).

## 7. Still to build (from the old program description)

Taken from `netGrepWin.code.md`; details per step in the roadmap.

- **File menu:** Open (select from last search queries), close this / all / all but this,
  open/save search results (search input + file list + token lines), each save adds to the
  last used files list; Last used search queries (click opens the query in a search form and
  starts the search); Last used search files (click opens the saved result without searching).
- **Search menu:** Search in Files.
- **Settings:** edit the two CSS files (step 33).
- **MDI commands and keys** act on the active result form.
- **Lists** are saved on each change.
- **Result form:**
  - File list: click on a file shows its lines in the lower pane and marks it as active;
    double click opens a "show and search inside file" form (step 28); Size and Date columns.
  - Matching lines: styling details compared with the old program are open (font size 9pt vs. 8pt).
- **Old drawbacks to avoid:** UI freezes while searching (→ async), results lost after a popup
  question, admin rights for config (→ `%AppData%`).
