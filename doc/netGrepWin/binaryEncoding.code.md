# netGrepWin: binary files and text encodings

Notes from reading the old code (read only), for step 25 of grepWinUi.

- **Reading:** `clsSearchInLines.DoSearchInLines` reads every file with
  `File.ReadAllLines(FileName, Encoding.Default)`, i.e. the ANSI code page of the PC (e.g. Windows-1252).
  `ReadAllLines` still detects a byte order mark (UTF-8 / UTF-16 / UTF-32), so files with BOM were read
  right; UTF-8 files without BOM showed umlauts wrong (e.g. `Ã¤` for `ä`). The whole file is read
  into memory at once.
- **Binary files:** no detection. A binary file was read like text (lines split at line breaks, NUL
  and control characters in the result HTML). `clsSearchProperties` only has the comment
  `// public bool bSkipBinaryFiles;` and a command line text `SkipBinaryFiles =` without code.
- **Only binary files:** not in the old program.
- An unreadable file: message box "File: … could not be opened".
- Replace (`clsReplaceInFile`) and the saved HTML / text result files also use `Encoding.Default`.
