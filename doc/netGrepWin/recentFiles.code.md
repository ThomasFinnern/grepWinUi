# netGrepWin: last used search files and "same search already open"

Notes from reading the old code (read only), for step 29 of grepWinUi.

## List
- `LibGeneral\clsLastOpenedFiles.cs`: `clsTokenListFixedLength<clsLastOpenedFiles>`, file
  `LastOpenedFilesList.txt` in the user config folder (`Global.UserLastOpenedFilesList`).
- One list for **query files and result files**. `frmNetGrepWinProgram.AssignLastUsedFileName(path)`
  (`AddUsedToken` + save) is called after Save search query, Save search result and when a file is opened
  (`OpenSearchFromFileName`), not for the automatic reopen on program start.

## Menu File → Last used search files
- `lastUsedSearchFilesToolStripMenuItem_Click`: the submenu is cleared and filled with the full paths
  (newest first) each time; also refilled when the File menu opens.
- Click → `OpenSearchFromFileName(path)`: if the file exists, first tried as result file
  (`clsResultsOfOneSearch.LoadClassFromFile`), else as query file (`clsSearchProperties.LoadClassFromFile`).
  A missing file does nothing (no message, stays in the list).

## Same search already open
- `FindQueryAlreadyOpened(SearchProperties.IdSearchQueryDistinguishView())`: ID = search string, file types
  and some options (whole words, match case, lines without match, stop after first match …).
- `OpenSearchFromSearchResult` / `OpenSearchFromSearchQuery`: an open form with the same ID is closed
  (`CloseRequest`, "Reload") and a new form opens (ToDo in the code: ask about unsaved changes).
  On the automatic reopen at program start an already open search is not loaded again.
