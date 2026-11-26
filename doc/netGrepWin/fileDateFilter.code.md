# netGrepWin: filter by file date

Notes from reading the old code (read only), for step 26 of grepWinUi.

- `frmSearchProperties` has the controls: checkbox `chkbUseDateTime` ("use date/time"), below it
  `chkbIsFileNotOlderThen` with a value box `txtbOlderThenValue` and a unit combo `cmbbOlderThenFaktor`,
  and `chkbDateRange` with `txtbRangeStart` (start date as text) and a label.
- `chkbUseDateTime_CheckedChanged` only shows / hides these controls; a ticked `chkbUseDateTime` opens
  the extra options part of the dialog (`IsExtraParameterSet`).
- Nothing is stored in `clsSearchProperties` and the search never filters by date: the feature was
  never built. Only the file list HTML shows a date: `FileInfo.CreationTime` (creation, not modification).
