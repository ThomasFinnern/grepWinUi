# netGrepWin: result modes

Notes from reading the old code (read only), for step 24 of grepWinUi.
Options of `clsSearchProperties`, used in `clsSearchInLines.DoSearchInLines` and `clsSearchExecute.ExecuteSearch`.

- **First match per file only** (`bStopAfterFirstMatch`): after the first matching line (with all its
  hits) the `break` leaves the line loop, the rest of the file is not read. Lines after the match are
  therefore not kept.
- **List files without match** (`bFilesWithNoMatch`): after searching a file, a file **with** a match is
  dropped and a file **without** one gets an empty `clsFileResults` (no lines). The result lists only
  the files without match.
- **Non matching lines** (`bLinesWithNoMatch`): a matching line is ignored; for a line without match a
  `clsLineResult` is built, but never added to the file result (bug), so the result stays empty.
- **Match whole line** (`bNeedsCompleteLine`, "complete line"): stored and read from the command line,
  never used by the search.
- **File names only (no lines)**: not in the old program (no property found).
- The file list "Matches" column and the summary count hits; none of the modes changes their texts.
