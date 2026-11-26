# Old search query model (`clsSearchProperties`) and its lists

Notes from `..\netGrepWin\NetGrepWin\clsSearchProperties.cs`, `clsGrepTokenListBase.cs`,
`clsSearchStringToken.cs`, `clsFileSpecificationToken.cs`, `clsSearchFoldersToken.cs`,
`..\LibGeneral\clsLastUsedSearchQueries.cs`.

## clsSearchProperties

- Search string; file types and folders as lists, edited as one space-separated string
  (`SearchFileTypesAsString`, `SearchFoldersAsString`; folders with spaces are quoted).
- Options (all bool): regular expression, whole words, match case, lines with no match,
  stop after first match (breaks the line loop: **per file**), files with no match,
  search in found files, needs complete line, delimited list, fixed width list,
  file name regex, search inside zip, skip file types (+ string), folder regex, recurse folders.
- Delimited separator / field number and fixed-width begin / size are stored as **strings**.
- Replace settings (replace string, confirm each, backup, backup folder, …) live in the same class.
- `ViewSetting` (Show/Keep lines before/after) and `ShowViewSelection` are part of it.
- Defaults: file types `*.*`, folder `c:\`, all options off.
- `Equals` (used by the recent list to find an existing entry) compares search string, file types,
  folders, the search string options and the replace settings; not compared: file regex, zip,
  skip file types, folder regex, recurse folders, view settings.
- `CompareTo` (recent list: "already on top?") compares `IdSearchQueryDistinguishView`.
- `IdSearchQueryDistinguishView`: short id text for window titles (`search::files::` + option letters
  like `Ww`, `Mc`).

## Token lists (recent-first lists)

- `clsGrepTokenListBase`: a used list (`TokenList`) plus a **fixed** list of pinned favourites;
  `MergedTokenList()` shows both in the dropdowns. `AddUsedToken` puts a used item on top.
- Max counts: search strings 35, file specifications 35, folders 40, backup folders 20 (base default 25).
- Files: `SearchStringList.txt`, …; past queries: `LastUsedSearchQueries.txt`
  (`clsObjectListFixedLength<clsSearchProperties>`).

## Save / open a query file (`frmNetGrepWinProgram`)

- File → Save search query: active result form's `SearchProperties.SaveClass2File(file)`;
  without an active result form nothing happens. Extension `*.prp` ("Search property files").
- File → Open search query: `OpenSearchFromFileName` tries the file first as a saved **result**
  (`clsResultsOfOneSearch`, opens it with results), else as a **query** → new result form with it.
- Both add the file to the last opened files list (`UserLastOpenedFilesList`, File → Last used search files)
  and store it as `Config.LastUsedSearchPropertyFileName`.
- `DialogStartFolder(lastFile)`: folder of the last used file if it exists, else the user folder.

Decisions for grepWinUi: [doc/dev/grepWinUicode.md](../dev/grepWinUicode.md).
