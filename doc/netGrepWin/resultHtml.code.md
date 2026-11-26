# netGrepWin: HTML of the result panes and Cancel Search

Notes from reading the old code (read only), for step 18 of grepWinUi.

## Files
- `NetGrepWin\clsSearchResultFileListAsHtml.cs`: page of the file list pane (`FoundFileNamesHtmlDocument`).
- `NetGrepWin\clsSearchResultTokenLinesAsHtml.cs`: page of the matching lines pane
  (`FoundFilesWithTokenLinesHtmlDocument`).
- `NetGrepWin\clsHtmlPage.cs`, `clsHtmlElement`, `clsHtmlStdElements`, `clsHtmlEncode.cs`: small own
  HTML element tree (doc type, elements, properties) written as text.
- CSS: `FileList.css` and `TokenList.css` in the user config folder (`clsAppPaths.UserCfgPathName`),
  linked with `<link rel="stylesheet" href="file:///…">`. Copies in `Install\`, registered as user
  config files in `clsMainGlobal` (`UserConfigFiles.CfgFileList.Add("FileList.css")`).

## File list page
- `<span class="HeaderFoundFiles">` with `<h1>Results for searched string: '…'</h1>` and
  `<h2>` = `clsSearchResultInfo.ToString4Html()`:
  " in files: '…'. Found n matches in n files from n files searched in folders: '…'."
- `<hr>`, then a table: header row `Filename`, `Folder`, `Matches`, `Size`, `Date/Time`
  (size and creation time read from the file with `FileInfo` when the page is built).
  Each cell has `onclick="onClickFileList(this)"` / `ondblclick="onDblClickFileList(this)"`, which call
  `window.external.webFileList_HtmlClick` / `…DoubleClick` (IE `WebBrowser` scripting).
- No files: `<span class="NotFound">Found no matches for '…'</span>`. Closing `<hr>`.

## Matching lines page
- Optional (view option "Title") `<span class="HeaderFoundToken"><h1>Token found inside files</h1></span>`, `<hr>`.
- Per file (option "File names"): `<span class="FileNameTitle">full path</span><br>`.
- Per matching line (option "File contents"): pre lines, the line, post lines (lines before / after).
  Line number (option "Line numbers") `<span class="LineNumber">000012: </span>` (format `D6`), then
  the text; each hit in `<span class="Token">`. Without option "Whole line" only the hits are written.
- No files: `<span class="NotFound">Found no matches</span>`.

## CSS (Install\FileList.css, Install\TokenList.css)
- `* { font-family: verdana; font-size: 8pt; color: #000000 }`.
- Headers: background `#4286CE`, white text, h1 12pt, h2 10pt.
- `FileNameTitle`: blue, bold, 10pt, line height 25px, blue top border.
- `LineNumber`: `#00007F`, bold. `Token`: green, bold. `tr.highlight td`: background `#DCDCDC`.

## Cancel Search
- Search → Cancel Search (`cancelSearchToolStripMenuItem_Click`) takes the active result form
  (`SearchActiveMdiFormOfTypeDoSearch`) and calls `frmSearchResultView.DoCancelSearch()`, which
  forwards to the search results object (`LocalSearchResults.DoCancelSearch()`).
