# netGrepWin: lines before / after a match (keep and show)

Notes from reading the old code (read only), for step 22 of grepWinUi.

## Two pairs of numbers (`clsSurroundingLinesPara`)
- `ViewSetting.Keep.LineNbrPreviousToMatch / LineNbrFollowingMatch`: lines kept ("prepared")
  while searching (search properties dialog).
- `ViewSetting.Show.LineNbrPreviousToMatch / LineNbrFollowingMatch`: lines shown (header line of the
  result form, "Pre. lines" / "Post. lines"); their `Maximum` is the keep value.

## Keeping lines while searching (`clsSurroundingFileLines`, one per file result)
- Every line goes to `AddLine(nr, line)`; a state machine with two dictionaries:
  - `ePreFound`: the last keep-before (+1) lines in `PreFound` (oldest removed).
  - On a match `OnMatchingLine(nr)`: the `PreFound` lines (without the match line) move to
    `FileLines`, state `ePostFound`.
  - `ePostFound`: the next keep-after lines go to `PostFound`, then to `FileLines`; back to `ePreFound`.
  - `FileEnd()` moves open post lines to `FileLines`.
- So `FileLines` holds only the non-matching lines near matches (a `Dictionary<long, string>`).
  Debug message boxes when the state is inconsistent.

## Showing (`clsSearchResultTokenLinesAsHtml.FileNameAndLinesAsHtml`)
- Per matching line: `PreLines(nr, showBefore, prevMatchNr, showAfter)` = up to showBefore kept lines
  before it, but only those not already shown as post lines of the previous match; then the match
  line, then `<br>` (an empty line after **every** match line); then `PostLines(nr, showAfter)`.
  Post lines are not cut at the next match (a following match within showAfter lines could show twice).
- Context lines: line number (`span.LineNumber`, `D6`) + text in a plain `span`, no own CSS class.
- Changing "Pre. lines" / "Post. lines" in the header (`numericUpDownPreLines_ValueChanged`) saves the
  view setting and rebuilds only the matching lines pane (`ShowResultTokenLinesAsHtml`), no new search.
