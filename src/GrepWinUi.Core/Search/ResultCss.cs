using SaveAndLoad;

namespace GrepWinUi.Core.Search;

/// <summary>CSS text of one result pane and where it came from.</summary>
/// <param name="Text">CSS text, never null.</param>
/// <param name="Source">User file, defaults file or built-in text (<see cref="LoadSource.NewObject"/>).</param>
/// <param name="Problem">Text if a file existed but could not be read, else null.</param>
public sealed record CssText(string Text, LoadSource Source, string? Problem);

/// <summary>
/// Style sheets of the two result panes, one file per pane (<see cref="FileListFileName"/>,
/// <see cref="MatchingLinesFileName"/>). Read order: user file (%AppData%\GrepWinUi) → standard file
/// (&lt;exe&gt;\Defaults) → built-in text. The standard files must equal the built-in texts (test).
/// </summary>
public static class ResultCss
{
    /// <summary>Style sheet of the file list pane.</summary>
    public const string FileListFileName = "FileList.css";

    /// <summary>Style sheet of the matching lines pane.</summary>
    public const string MatchingLinesFileName = "MatchingLines.css";

    /// <summary>Reads the CSS of <paramref name="fileName"/>; never throws.</summary>
    public static CssText Load(StorageFolders folders, string fileName)
    {
        string? problem = null;
        foreach ((string folder, LoadSource source) in new[]
        {
            (folders.UserFolder, LoadSource.UserFile),
            (folders.DefaultsFolder, LoadSource.DefaultsFile),
        })
        {
            string path = Path.Combine(folder, fileName);
            try
            {
                if (File.Exists(path))
                {
                    return new CssText(File.ReadAllText(path), source, problem);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                problem ??= $"Style sheet '{path}' not readable: {ex.Message}";
            }
        }
        return new CssText(BuiltIn(fileName), LoadSource.NewObject, problem);
    }

    /// <summary>Built-in CSS of <paramref name="fileName"/> (empty for unknown names).</summary>
    public static string BuiltIn(string fileName) => fileName switch
    {
        FileListFileName => FileList,
        MatchingLinesFileName => MatchingLines,
        _ => "",
    };

    /// <summary>Built-in CSS of the file list pane (same as Defaults\FileList.css).</summary>
    public const string FileList = """
        /* Style sheet of the file list pane of grepWinUi.
           Your own copy: %AppData%\GrepWinUi\FileList.css (used instead of this file). */

        body {
            font-family: Verdana, sans-serif;
            font-size: 8pt;
            color: #000000;
            margin: 4px;
        }

        /* Header: searched string (h1) and summary (h2) */
        .HeaderFoundFiles {
            display: block;
            background-color: #4286CE;
            padding: 2px 4px;
        }

        .HeaderFoundFiles h1 {
            font-size: 12pt;
            color: white;
            margin: 0;
        }

        .HeaderFoundFiles h2 {
            font-size: 10pt;
            color: white;
            margin: 0;
        }

        .Problem {
            color: #B00000;
        }

        .NotFound {
            color: #B00000;
            font-weight: bold;
        }

        /* File table: File name, Folder, Matches. Every column is as small as its content
           (File name at most 30em) and left aligned, also columns added later. */
        table {
            border-collapse: collapse;
        }

        th {
            text-align: left;
            border-bottom: 1px solid gray;
            padding: 1px 12px 1px 0;
            white-space: nowrap;
        }

        td {
            text-align: left;
            padding: 1px 12px 1px 0;
            white-space: nowrap;
        }

        /* Very long file names are cut with "…" (full name as tooltip) */
        td.FileName div {
            max-width: 30em;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        tr:hover td {
            background-color: #DCDCDC;
        }

        /* The clicked file (its lines are shown in the matching lines pane) */
        tr.highlight td {
            background-color: #C8DCF4;
        }
        """;

    /// <summary>Built-in CSS of the matching lines pane (same as Defaults\MatchingLines.css).</summary>
    public const string MatchingLines = """
        /* Style sheet of the matching lines pane of grepWinUi.
           Your own copy: %AppData%\GrepWinUi\MatchingLines.css (used instead of this file). */

        body {
            font-family: Verdana, sans-serif;
            font-size: 9pt;
            color: #000000;
            margin: 4px;
        }

        /* Header line of the pane */
        .HeaderFoundToken {
            display: block;
            background-color: #4286CE;
            padding: 2px 4px;
        }

        .HeaderFoundToken h1 {
            font-size: 12pt;
            color: white;
            margin: 0;
        }

        .NotFound {
            color: #B00000;
            font-weight: bold;
        }

        /* Full path of a file above its lines */
        .FileNameTitle {
            display: block;
            margin-top: 6px;
            line-height: 25px;
            border-top: 1px solid blue;
            font-weight: bold;
            font-size: 10pt;
            color: blue;
        }

        /* One matching line: line number and text */
        .Line {
            white-space: pre;
        }

        /* View option "Fixed font" (body class) */
        .FixedFont .Line {
            font-family: Consolas, monospace;
        }

        .LineNumber {
            color: #00007F;
            font-weight: bold;
        }

        /* A hit of the search string */
        .Token {
            color: green;
            font-weight: bold;
        }

        /* A line before / after a matching line (header "Pre. lines" / "Post. lines") */
        .ContextLine {
            color: #606060;
        }

        /* Space between groups of lines that don't follow each other */
        .Gap {
            height: 0.8em;
        }
        """;
}
