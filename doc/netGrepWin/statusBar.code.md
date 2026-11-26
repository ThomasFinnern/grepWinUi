# netGrepWin: status bar while searching

Notes from reading the old code (read only), for step 19 of grepWinUi.

## Where
- The status bar is part of each result form (`frmSearchResultView`), not of the MDI main window.
  `frmNetGrepWinProgram` has only an unused `toolStripStatusLabel1`.
- Fields (`frmSearchResultView.Designer.cs`): label "Title: " + `toolStripStatusLblTitleText`
  ("untouched"), label "Info: " + `toolStripStatusActInfoText` ("Actual Info"), `toolStripProgressBar1`
  (100 × 16, never set), "Folder:" + `toolStripStatusLblFolderNbrText`, "Files:" +
  `toolStripStatusLblFileNbrText`, "Time:" + `toolStripStatusLbTimeText`.

## How it is filled
- `clsSearchResults` raises events `OnSetTitle`, `OnSetInfo`, `OnUpdateFolderCount`,
  `OnUpdateFileCount`, `OnUpdateTime` (one string message each); the form sets the label text and calls
  `Application.DoEvents()` (the search runs on the UI thread, so the window froze in between).
- A 250 ms timer (`timerUpdateInfos_Tick`) was planned, its code is commented out.
- `clsSearchExecute.ExecuteSearch`:
  - Title "Collect files from request", then "Search for and inside files", at the end "Searching done".
  - Per file: Info = full file name (only if config `bViewFileNamesOnSearch`), Folder = number of
    folders searched so far (only if `bCountFoldersOnSearch`), Files = number of files so far
    (only if `bCountFilesOnSearch`). Both counts again at the end.
  - Time: "-%-" at the start, at the end the elapsed time as `s7.123`, `m2.07.123` or `h1:2.07.123`.
- `ResetStripStatus()` empties Title and Info.

## Differences in grepWinUi (see CLAUDE.md "UI spec: status bar")
- One status bar in the main window; it shows the state of the active result form.
- Folder shows the last name of the folder being searched (not a count).
- Files shows read / all; Time counts up while searching; Working is a marquee while searching.
