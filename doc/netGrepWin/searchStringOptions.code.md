# netGrepWin: search string options (Match case, Whole words only, Regular expression)

Notes from reading the old code (read only), for step 21 of grepWinUi.

## Building the expression (`clsSearchRegularExpression.CreateSearchRegEx`)
- Every search is a .NET `Regex`, also plain text.
- Without "Regular expression" (`bUseRegularExpression`) the search string is escaped (`Regex.Escape`).
- "Whole words only" (`bWholeWordsOnly`): `\b` + string + `\b`. A comment notes that `\b` fails for
  strings that start or end with a non-word character (e.g. `.NET`, `C#`) and suggests
  `(?:^|\W)` / `(?:\W|$)`, not used. No group around the string, so an alternation `a|b` became
  `\ba|b\b`.
- "Match case" (`bMatchCase`): without it `RegexOptions.IgnoreCase`. No other options (culture
  dependent case rules, no timeout).
- An invalid expression: message box "Error in RegEx string: …", the search does not run
  (`ExecuteSearch` returns when the `Regex` is null).

## Finding the hits (`clsSearchInLines.DoSearchInLines`)
- Per line `regExSearch.Match(Line, LastIdx)`; for each success one hit (start, length), then
  `LastIdx = start + length` and the next `Match`. A match of length 0 (e.g. `^` or `a*`) would never
  move on (endless loop).
- Options of later steps are handled here too: "Non matching lines" (`bLinesWithNoMatch`),
  "First match per file only" (`bStopAfterFirstMatch`), "List files without match" (`bFilesWithNoMatch`).
