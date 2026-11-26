# netGrepWin: file and folder filters

Notes from reading the old code (read only), for step 23 of grepWinUi.

## Finding the files (`clsSearchFilesInFoldersIEnumerator.IenumFiles`)
- Outer loop over the file patterns (`FileSpecificationAsList()`: the file names text split at blanks),
  inner loop over the folders (`SearchFolders`); a file matching two patterns is found twice.
- A missing folder: message box "Wrong folder : '…'" and the next folder.
- `enumFilesFoundInFolders(dir, pattern)`: `Dir.GetFiles(pattern)`, then (if `bDoRecourseFolders`,
  "Include subfolders") each subfolder recursively. No try/catch: a folder without access ends the search
  with an exception.
- File names as RegEx (`bUseFileRegularExpression`): each blank separated part is a `Regex` (no options,
  so case sensitive) and `Match` (part of the name is enough) on the file name. The flag exists in
  `clsSearchProperties` but the enumerator's flag is never set from it, so it never worked.
- Folder names as RegEx (`bUseFolderRegularExpression`, checkbox "RegEx full path name" in
  `frmSearchProperties`): stored, given on the command line, but never used by the search.
- With "Only in files of current result" (`bSearchInFoundFiles`) the files of the last result are used
  instead (`UseFileList`).

## Several folders (`clsSearchProperties.SearchFoldersAsString`)
- The folder text is split by `LibStringFileFolderNames.ExtractFolderListWithApostrophes`: names in
  double quotes are one folder (may hold blanks), the other parts are split at blanks.
- Written back with quotes around names that contain blanks (`CreateSpaceSeparatedFolderNamesString`).

## Not in the old program
- Skip by extension, Skip folders, Base name as RegEx: only in the new search dialog.
