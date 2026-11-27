# netGrepWin: clicks in the result panes and the file content view

Notes from reading the old code (read only), for step 28 of grepWinUi.

## Click on a file in the file list
- `clsSearchResultFileListAsHtml.cs`: each file name cell has `onclick="onClickFileList(this)"` and
  `ondblclick="onDblClickFileList(this)"`; a page script forwards them to the form with
  `window.external.webFileList_HtmlClick` / `…HtmlDoubleClick` (IE `WebBrowser` scripting,
  `ObjectForScripting = this` in `frmSearchResultView.AssignWebRowserParentOwner`).
- `frmSearchResultView.webFileList_HtmlClick`: takes file name and folder from the first two cells of the
  row (`Document.ActiveElement.Parent`), looks for the `span` in the matching lines page whose
  `InnerHtml` equals the full path and calls `ScrollIntoView(true)`.
  Then `FileNameList_HighLightOneName` sets the class `highlight` on that row and removes it from all
  other rows (CSS `tr.highlight td { background-color: #DCDCDC }`).

## Double click: "File content view" (`frmSearchResultRichTextView`)
- Opened with `Show(this)` (owned, floating window), gets the full path and the search results.
- Reads the whole file again (`File.ReadAllLines`, ANSI code page) into a `RichTextBox`; a second
  `RichTextBox` holds the line numbers. Hits of the stored result are green and bold
  (`clsSearchResultTokenLinesAsRtf`).
- Buttons: `<` / `>` previous / next hit (labels "Start / End of file reached. Press again …"),
  `<<` / `>>` previous / next file of the result, Edit, Open (no code behind them), Exit.
- grepWinUi: not in step 28; a later step chooses the approach (viewer, own editor, Monaco,
  Scintilla, external editor; see the roadmap in CLAUDE.md).
