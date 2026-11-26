# netGrepWin: "Count files first" and progress in %

Notes from reading the old code (read only), for step 20 of grepWinUi.

- `clsSearchProperties` lists a parameter `CountFilesFirst` (comment only, no value), and
  `frmSearchProperties` has a "Count files first" checkbox whose loading is commented out
  (`//CountFilesFirst.Checked = SearchProperties;`). The search never reads it.
- The progress bar of the result form's status bar (`toolStripProgressBar1`) is never set.
- Config `bCountFilesOnSearch` ("While searching" options page) only decides whether the Files field
  shows the running file number (`clsSearchFilesInFoldersIEnumerator.FileNbr`); there is no total.
- Options page "While searching" has a checkbox "*View progressbar" (marked as not working).
- `ExecuteSearch` sets the title "Collect files from request" before the search, but that only prepares
  folder and file name patterns (`AssignFolderAndFilesFromRequest`); files are found while searching.

So the feature is new in grepWinUi: see `doc/dev/grepWinUicode.md`.
