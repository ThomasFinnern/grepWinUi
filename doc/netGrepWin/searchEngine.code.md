# Old search engine

Notes from `..\netGrepWin\NetGrepWin\clsSearchExecute.cs`, `clsSearchFilesInFoldersIEnumerator.cs`,
`clsSearchInLines.cs`, `clsSearchRegularExpression.cs`, `clsSearchResults.cs`, `clsSearchResultInfo.cs`.
Decisions for grepWinUi: [doc/dev/grepWinUicode.md](../dev/grepWinUicode.md).

## Flow (`clsSearchExecute.ExecuteSearch`)

1. Folders and file types from the query; `clsSearchResultInfo` for the counts.
2. One `Regex` for the search string (`CreateSearchRegEx`):
   - not "Use regular expression" → `Regex.Escape(searchString)`
   - "Whole words only" → `\b` + string + `\b`
   - "Match case" off → `RegexOptions.IgnoreCase`
   - invalid regex → message box, no search
3. `foreach` file of the enumerator: status texts (title, info = file name, folder / file count),
   `DoSearchInLines`, add the file result. "Files with no match": only files without hit are kept.
4. Stopwatch for the time text; counts: matched tokens, matched files, searched files.
5. Runs on the UI thread (no async) → the window freezes while searching.

## Files (`clsSearchFilesInFoldersIEnumerator.IenumFiles`)

- Loop over **file patterns**, then over **folders** (a file matching two patterns comes twice).
- Missing folder → message box "Wrong folder".
- Wildcards: `DirectoryInfo.GetFiles(pattern)`; RegEx file names: all files, `Regex.Match(name)`.
- Recursion with `GetDirectories()` when "recourse folders"; counts folders and files.
- No handling of access errors (an inaccessible folder stops the search with an exception).
- A debug leftover (`if (NamePart.Contains("cs"))`) only returns files whose name contains "cs".
- Cancel: `bCancelSearch` flag checked in the loops.

## Lines (`clsSearchInLines.DoSearchInLines`)

- `File.ReadAllLines(file, Encoding.Default)` (ANSI code page); unreadable file → message box.
- Line numbers start at 1. Every hit in a line is stored (start index, length) — several hits per line.
- "Stop after first match": stops the file after the first matching line.
- "Lines with no match": stores the lines without hit instead.
- Lines around a match: `clsSurroundingFileLines` keeps the previous / following lines (Keep counts).

## Result model

- `clsSearchResults.Files2LineResults`: list of `clsFileResults` (file name, `LinesResultInfos`).
- `clsLineResult`: `Line`, `LineIdx`, `ResultInfos` (list of `StartIdx` / `Length`).
- `clsSearchResultInfo`: query + `TokenMatchedNbr`, `FilesMatchedNbr`, `FileSearchedNbr`, `FilesSkippedNbr`.
- Status updates through events (`OnSetTitle`, `OnSetInfo`, `OnUpdateFolderCount`, `OnUpdateFileCount`, `OnUpdateTime`).
