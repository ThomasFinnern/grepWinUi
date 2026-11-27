# netGrepWin: view options of the result form

Notes from reading the old code (read only), for step 27 of grepWinUi.

## Files
- `NetGrepWin\clsSearchViewProperties.cs`: the flags `bDoShowTitle`, `bDoShowLineNumbers`,
  `bDoShowFixedFont`, `bDoShowFileNames`, `bDoShowFileContents`, `bDoShowWholeLine`,
  `bDoShowDoubleBlanks`. Default: all true except Double blanks (not set, so false).
  The copy constructor forgets `bDoShowDoubleBlanks`.
- `NetGrepWin\frmSearchResultView.cs`: header line checkboxes (`AssignConfig2Control` /
  `AssignControl2Config`); a change builds the matching lines page again. Config also has a
  `ShowViewSelection` set (which checkboxes are visible), but its use is commented out.
- `NetGrepWin\frmOptions.cs`: standard values in the options dialog (page "search result view").
- `NetGrepWin\clsSearchResultTokenLinesAsHtml.cs`: uses the flags when building the page.

## What each option does (matching lines pane only; the file list ignores them)
- **Title:** `<span class=HeaderFoundToken><h1>Token found inside files</h1></span>` and `<hr>`.
- **File names:** `<span class="FileNameTitle">full path</span><br>` per file.
- **File contents:** all lines of the file (pre lines, matching line, post lines). With File names
  on and File contents off the page is a list of file paths.
- **Line numbers:** `<span class="LineNumber">000012: </span>` in front of matching and pre/post lines.
- **Whole line:** off → only the hits of a matching line are written, `Token` spans directly one after
  the other (no separator). Pre/post lines are always written whole.
- **Fixed font**, **Double blanks:** stored and shown, but never used when building the page.
