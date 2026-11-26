Discussion: parameters other grep tools have that we don't

Compared with grepWin, dnGrep, AstroGrep, Agent Ransack and ripgrep. My suggestions, in priority order:

1. Text encoding: auto-detect, or force UTF-8 / UTF-16 / ANSI code page. This matters a lot for old ANSI files.
2. Include hidden / system files (off by default).
3. File size limit: min/max, e.g. skip files over 10 MB.
4. Max folder depth for subfolders.
5. Multiline RegEx: a match may span several lines (. also matches a line break).
6. Max matches per file, a general form of "first match per file only" (grep -m n).
7. File date basis: modified or created, for the date filter.
8. Search file names only, without opening the files (find files by name). This could also simply be "empty search string".
9. Count only: number of matches per file, no lines (grep -c).
10. Maximum shown line length: shorten very long lines, e.g. in minified JS/CSS.

Ideas for later: AND/OR/NOT of several terms (dnGrep, Agent Ransack), respecting .gitignore (ripgrep), smart case (case-sensitive only if the search text has capitals), search in Office/PDF files (dnGrep), and a backup before replacing (grepWin, for the Replace step).
