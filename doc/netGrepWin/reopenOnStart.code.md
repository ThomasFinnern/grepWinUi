# netGrepWin: reopen the last searches on start

Notes from reading the old code (read only), for step 30 of grepWinUi.

## On close (`frmNetGrepWinProgram_FormClosing`)
- The active result form (`SearchActiveMdiFormOfTypeDoSearch`) is saved as `SearchResult.Res`
  (`clsResultsOfOneSearch`: search properties + results) in the user config folder.
- Every open result form is saved as `OpenSearchResult_<i>.res`; the list of these files goes into
  `ReopenFiles.txt` (`clsReopenClosedFiles`). Always done, whatever the settings.
- Result files can be large: the next start may take 10+ s.

## On start (`NetGrepWinProgram_Load`)
- Command line searches are shown first.
- `Config.bDoLoadLastOpenSearchesOnStart` (or command line) → `OpenSearchesActiveAtClosing`: every file of
  `ReopenFiles.txt` with `OpenSearchFromFileName(path, true)` (a search that is already open is not loaded
  again).
- Otherwise (unless the command line says no) `OpenLastUsedSearchResults`: only `SearchResult.Res`.
- Exactly one open form → maximized.

## Settings page "On start of program" (`CtrlOptionOnStartOfProgram`)
- `bDoPopUpSearchOnStart` ("Open search form"): stored, set from the command line, not used on start.
- `bDoRepeatLastSearchOnStart`: the first new search form starts its search at once (no dialog,
  `bDoRequestRestartSearch`) — grepWinUi step 30b.
- `bDoLoadLastOpenSearchesOnStart` ("Reopen last open searches"): see above.
- `bDoViewLastUsedSearchResultOnStart` ("Show last search result"): stored only; the old program showed
  the last result whenever it did not reopen all searches.
