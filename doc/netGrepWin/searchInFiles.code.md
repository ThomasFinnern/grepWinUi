# netGrepWin: search in found files and restart search

Notes from reading the old code (read only), for step 31 of grepWinUi.

## "Only in files of current result" (`clsSearchProperties.bSearchInFoundFiles`)
- Checkbox `chkSearchInFoundFiles` in `frmSearchProperties`; also a command line option `SearchInFoundFiles`.
- `frmSearchResultView.ExecuteSearchFromForm`: with the option, the file results of the old search
  (`OldSearchResults.Files2LineResults`) are handed to the new search object.
- `clsSearchResults.AssignFolderAndFilesFromRequest`: without the option or without old files: folders,
  file types and subfolders of the query. With old files: `UseFileList` = the old file names; file types and
  folders are not used (subfolder flag set but meaningless). The old results are cleared.
- The option stays in the query, so every further search narrows the list again.

## Search → Search in Files (`searchInFilesToolStripMenuItem_Click`)
- Active result form: `bSearchInFoundFiles = true`, then its search properties dialog
  (`RequestPropertiesInputDoSearchAndShow`). The flag is set before the dialog, so it stays after Cancel.
- Without one: message "Actual search form not found. Will open new search", then New search.

## Search → Restart search (`restartSearchToolStripMenuItem_Click`, Alt+R)
- Active result form: `ExecuteSearchFromForm()` + `ShowResults()` (no dialog). Without one: New search.
- Keys: Alt+R in the result form (`frmSearchResultView` key handler and `ProcessCmdKey`), Ctrl+R in an
  older key handler.
