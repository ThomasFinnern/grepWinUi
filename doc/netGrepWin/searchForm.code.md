# Old search properties form (`frmSearchProperties`)

Notes from `..\netGrepWin\NetGrepWin\frmSearchProperties.cs` (+ Designer) and its callers.
Screenshot: `searchForm.png`.

## How the form is opened (from outside the result form)

- The form is always opened by a result form (`frmSearchResultView.RequestSearchPropertiesInput`):
  `new frmSearchProperties(LocalSearchProperties).ShowDialog(this)` → modal over that result form.
  The result form passes **its own** search properties object; the dialog edits it directly.
- After closing, the result form reads `bDoStartSearch`. If true: remember as last query,
  set the view's pre/post line numbers (and their maximum = Keep lines), update the title, search, show.
- Who asks the result form to open it:
  - **Search → Search (Ctrl+F)** in the main window: finds the active MDI child
    (`SearchActiveMdiForm`: `ActiveMdiChild`, else the active child of any open MDI container).
    If it is a result form → open the dialog there. Otherwise → **File → New search**.
  - **File → New search (Ctrl+N)**: creates a result form with properties loaded from the user file
    (= last used query) and a copy of the config view settings; sets `RequestSearchProperties = true`.
    The result form opens the dialog in its `Shown` event. (Exception: first start with
    "repeat last search on start" → restart the search instead of asking.)
  - **Search → Search in Files**: like Search, but sets `bSearchInFoundFiles = true` first;
    without an active result form only a message box.
  - **Main window `Shown`** (program start): opens the dialog for the last active result form,
    if there is one. Otherwise (decided for grepWinUi): a new empty result form with the dialog open.
  - **Ctrl+F inside the result form's HTML panes**: opens the dialog directly.

## Buttons and keys

- **Start search** (also Enter): write controls → properties, save properties file, `bDoStartSearch = true`, close.
- **Exit**: write controls → properties (keep changes), close, no search.
- **Cancel** (also Esc): close, discard changes.
- Writing back also adds the search string, file spec and folder to their recent lists and saves them.

## Layout (top to bottom)

Green title bar "Search properties". Three sections, each a bold label, a `[+]` toggle and a
combo box with a `...` button. `[+]` shows/hides the section's extra options; it starts open
when any of them differs from its default.

1. **Search string** (combo: current value first, then the recent list; `...` = token list)
   - Whole words only, Match Case, Use Regular expression, Stop after first Match,
     Files with no match, Lines with no match, Search in found files
   - Fixed width list → Begin position, Size (shown only when checked)
   - Delimited List → Separation character, Search field number (shown only when checked)
   - `*`Use date and time → Not older than [value] [Seconds…Years], Date range from – to (not coded)
   - Hidden: Complete line, Show file names only
2. **File types** (combo, `...` = file type list editor)
   - RegEx-file names; Search in Zip files (disabled)
   - Hidden/disabled: Skip binary files, Skip text files, Skip file types + combo
3. **Folders** (combo, `...` = folder list editor; drag & drop of folders from Explorer
   → space-separated folder names)
   - Recourse folders (default on; section opens when off)
   - Disabled: RegEx-folder names, RegExp path and file name / RegExp path name,
     RegEx on last path part, Count files first; hidden: Skip folder types

Bottom: Show previous / following lines of match, Prepare (= Keep) previous / following lines of match,
then Cancel, Start search, Exit.
