using System.Net;
using System.Text;
using GrepWinUi.Core.AppData.Config;
using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// HTML pages of the two result panes: the file list and the matching lines. The CSS text of the pane
/// (<see cref="ResultCss"/>) is put into the page as &lt;style&gt;, after a background rule, so the CSS
/// can override the background. CSS class names as in the old program. All texts are HTML encoded.
/// Very long results are cut (WebView2 shows at most about 2 MB of HTML).
/// </summary>
public static class ResultPageHtml
{
    /// <summary>Most matching lines written to the page.</summary>
    public const int MaxLines = 5000;

    /// <summary>Page with one hint text (before the first search, while searching, on errors).</summary>
    public static string Hint(string text, string css, string background) =>
        Page(css, background, text.Length == 0 ? "" : $"<p>{Encode(text)}</p>");

    /// <summary>
    /// File list pane: header (searched string, <see cref="Summary"/>), problems, then a table
    /// File name / Folder / Matches (Lines with "Non matching lines", none without lines), or "Found no …".
    /// Each row has <c>data-file</c> = index in <see cref="SearchResult.Files"/>; the page script
    /// <see cref="ResultPageScript.FileList"/> reports clicks on rows.
    /// </summary>
    public static string FileList(SearchResult result, string css, string background)
    {
        StringBuilder body = new();
        body.Append("<div class=\"HeaderFoundFiles\"><h1>Results for searched string: '")
            .Append(Encode(result.Query.SearchString)).Append("'</h1><h2>")
            .Append(Encode(Summary(result))).Append("</h2></div>");
        foreach (string problem in result.Problems)
        {
            body.Append("<p class=\"Problem\">").Append(Encode(problem)).Append("</p>");
        }

        if (result.Files.Count == 0)
        {
            body.Append(NotFound(result));
            return Page(css, background, body.ToString());
        }

        ResultMode mode = ResultMode.Of(result.Query);
        string? countTitle = mode.NoLines ? null : mode.NonMatchingLines ? "Lines" : "Matches";
        body.Append("<table><tr><th class=\"FileName\">File name</th><th class=\"Folder\">Folder</th>")
            .Append(countTitle is null ? "" : $"<th class=\"Matches\">{countTitle}</th>").Append("</tr>");
        for (int index = 0; index < result.Files.Count; index++)
        {
            FileResult file = result.Files[index];
            string name = Encode(Path.GetFileName(file.Path));
            body.Append("<tr data-file=\"").Append(index).Append("\"><td class=\"FileName\"><div title=\"").Append(name).Append("\">").Append(name)
                .Append("</div></td><td class=\"Folder\">").Append(Encode(Path.GetDirectoryName(file.Path) ?? ""))
                .Append("</td>")
                .Append(countTitle is null ? "" : $"<td class=\"Matches\">{mode.CountOf(file)}</td>").Append("</tr>");
        }
        body.Append("</table>");
        return Page(css, background, body.ToString(), script: ResultPageScript.FileList);
    }

    /// <summary>
    /// Matching lines pane: header (view option Title), then per file its path (<c>FileNameTitle</c>, option
    /// File names) and its lines (option File contents): <c>LineNumber</c> (option Line numbers) + text with
    /// <c>Token</c> hits. Around each matching line up to <see cref="ResultViewOptions.ShowLinesBefore"/> /
    /// <see cref="ResultViewOptions.ShowLinesAfter"/> kept lines (<c>Line ContextLine</c>), each line once;
    /// lines after a match stop at the next match. Groups of lines that don't follow each other are parted
    /// by <c>Gap</c> (only when lines before / after are shown). Without option Whole line only the hits of a
    /// matching line are written (<see cref="HitsOnly"/>); without option Double blanks runs of blanks become
    /// one (<see cref="ReduceBlanks(LineMatch)"/>). Option Fixed font: <c>&lt;body class="FixedFont"&gt;</c>.
    /// Each file starts with an element with id <see cref="ResultPageScript.FileId"/> (the file name title,
    /// or an empty span without option File names), the target of a click in the file list.
    /// "Found no matches" when there are no files.
    /// </summary>
    public static string MatchingLines(SearchResult result, ResultViewOptions view, string css, string background)
    {
        StringBuilder body = new();
        string? bodyClass = view.FixedFont ? "FixedFont" : null;
        if (view.ShowTitle)
        {
            body.Append("<div class=\"HeaderFoundToken\"><h1>").Append(LinesTitleKind(result.Query)).Append(" '")
                .Append(Encode(result.Query.SearchString)).Append("'</h1></div>");
        }
        if (result.Files.Count == 0)
        {
            body.Append(NotFound(result));
            return Page(css, background, body.ToString(), bodyClass);
        }

        int written = 0;
        for (int index = 0; index < result.Files.Count; index++)
        {
            FileResult file = result.Files[index];
            string id = ResultPageScript.FileId(index);
            body.Append(view.ShowFileNames
                ? $"<div class=\"FileNameTitle\" id=\"{id}\">{Encode(file.Path)}</div>"
                : $"<span id=\"{id}\"></span>");
            if (!view.ShowFileContents)
            {
                continue;
            }
            foreach (ShownLine shown in ShownLines(file, Math.Max(0, view.ShowLinesBefore), Math.Max(0, view.ShowLinesAfter)))
            {
                if (shown.GapBefore)
                {
                    body.Append("<div class=\"Gap\"></div>");
                }
                if (written++ == MaxLines)
                {
                    body.Append($"<p class=\"NotFound\">… only the first {MaxLines} lines are shown.</p>");
                    return Page(css, background, body.ToString(), bodyClass);
                }
                body.Append(LineHtml(shown, view));
            }
        }
        return Page(css, background, body.ToString(), bodyClass);
    }

    /// <summary>One shown line as &lt;div&gt; with line number and text, formed by the view options.</summary>
    public static string LineHtml(ShownLine shown, ResultViewOptions view)
    {
        string number = view.ShowLineNumbers ? $"<span class=\"LineNumber\">{LineNumber(shown.LineNumber)}: </span>" : "";
        if (shown.Match is LineMatch match)
        {
            LineMatch line = view.ShowDoubleBlanks ? match : ReduceBlanks(match);
            string text = view.ShowWholeLine ? MarkHits(line) : HitsOnly(line);
            return $"<div class=\"Line\">{number}{text}</div>";
        }
        string context = view.ShowDoubleBlanks ? shown.Text : ReduceBlanks(shown.Text);
        return $"<div class=\"Line ContextLine\">{number}{Encode(context)}</div>";
    }

    /// <summary>Text between hits shown without view option Whole line.</summary>
    public const string HitSeparator = " … ";

    /// <summary>
    /// Only the hits of a line, each in &lt;span class="Token"&gt;, parted by <see cref="HitSeparator"/>.
    /// A line without hits (e.g. "Non matching lines") is written whole.
    /// </summary>
    public static string HitsOnly(LineMatch line) => line.Hits.Count == 0
        ? Encode(line.Text)
        : string.Join(HitSeparator, line.Hits.Select(hit =>
            $"<span class=\"Token\">{Encode(line.Text.Substring(hit.Start, hit.Length))}</span>"));

    /// <summary>Text with every run of blanks (spaces, tabs) reduced to one space.</summary>
    public static string ReduceBlanks(string text) => ReduceBlanks(new LineMatch(0, text, [])).Text;

    /// <summary>
    /// Line with every run of blanks (spaces, tabs) reduced to one space; the hits are moved so they mark
    /// the same text (hits that shrink to nothing are dropped).
    /// </summary>
    public static LineMatch ReduceBlanks(LineMatch line)
    {
        string text = line.Text;
        StringBuilder reduced = new(text.Length);
        int[] newIndex = new int[text.Length + 1];
        for (int index = 0; index < text.Length; index++)
        {
            newIndex[index] = reduced.Length;
            bool blank = IsBlank(text[index]);
            if (!blank)
            {
                reduced.Append(text[index]);
            }
            else if (index == 0 || !IsBlank(text[index - 1]))
            {
                reduced.Append(' ');
            }
        }
        newIndex[text.Length] = reduced.Length;
        if (reduced.Length == text.Length && !text.Contains('	'))
        {
            return line;
        }

        List<Hit> hits = [];
        foreach (Hit hit in line.Hits)
        {
            int start = newIndex[hit.Start];
            int length = newIndex[hit.Start + hit.Length] - start;
            if (length > 0)
            {
                hits.Add(new Hit(start, length));
            }
        }
        return line with { Text = reduced.ToString(), Hits = hits };
    }

    private static bool IsBlank(char character) => character is ' ' or '	';

    /// <summary>A line of the matching lines pane: a matching line (<see cref="Match"/> set) or a kept line.</summary>
    /// <param name="LineNumber">Line number in the file.</param>
    /// <param name="Text">Text of the line.</param>
    /// <param name="Match">The matching line, null for a line before / after a match.</param>
    /// <param name="GapBefore">True when lines are skipped between the previous shown line and this one.</param>
    public sealed record ShownLine(int LineNumber, string Text, LineMatch? Match, bool GapBefore);

    /// <summary>
    /// Lines of one file in the order they are shown: per matching line up to <paramref name="before"/>
    /// kept lines before it (not shown yet), the matching line, up to <paramref name="after"/> kept lines
    /// after it (before the next matching line). <see cref="ShownLine.GapBefore"/> only when
    /// <paramref name="before"/> or <paramref name="after"/> is above 0.
    /// </summary>
    public static IEnumerable<ShownLine> ShownLines(FileResult file, int before, int after)
    {
        Dictionary<int, string> kept = file.ContextLines.ToDictionary(line => line.LineNumber, line => line.Text);
        bool marksGaps = before > 0 || after > 0;
        int lastShown = 0;
        for (int index = 0; index < file.Lines.Count; index++)
        {
            LineMatch match = file.Lines[index];
            int nextMatch = index + 1 < file.Lines.Count ? file.Lines[index + 1].LineNumber : int.MaxValue;
            List<ShownLine> group = [];
            for (int number = Math.Max(match.LineNumber - before, lastShown + 1); number < match.LineNumber; number++)
            {
                if (kept.TryGetValue(number, out string? text))
                {
                    group.Add(new ShownLine(number, text, null, false));
                }
            }
            group.Add(new ShownLine(match.LineNumber, match.Text, match, false));
            long lastAfter = Math.Min((long)match.LineNumber + after, nextMatch - 1L);
            for (int number = match.LineNumber + 1; number <= lastAfter; number++)
            {
                if (kept.TryGetValue(number, out string? text))
                {
                    group.Add(new ShownLine(number, text, null, false));
                }
            }

            foreach (ShownLine line in group)
            {
                bool gap = marksGaps && lastShown > 0 && line.LineNumber > lastShown + 1;
                yield return gap ? line with { GapBefore = true } : line;
                lastShown = line.LineNumber;
            }
        }
    }

    /// <summary>Line number with six digits as in the old program, e.g. "000012".</summary>
    public static string LineNumber(int number) => number.ToString("D6");

    /// <summary>One line text with every hit wrapped in &lt;span class="Token"&gt;.</summary>
    public static string MarkHits(LineMatch line)
    {
        StringBuilder text = new();
        int position = 0;
        foreach (Hit hit in line.Hits)
        {
            text.Append(Encode(line.Text[position..hit.Start]))
                .Append("<span class=\"Token\">").Append(Encode(line.Text.Substring(hit.Start, hit.Length))).Append("</span>");
            position = hit.Start + hit.Length;
        }
        return text.Append(Encode(line.Text[position..])).ToString();
    }

    /// <summary>
    /// E.g. "In files '*.cs': found 12 matches in 3 files from 57 files searched in folder 'c:\src' (0.4 s)"
    /// (counts per result mode: <see cref="ResultMode.Counts"/>); " — cancelled" at the end when the search was stopped.
    /// </summary>
    public static string Summary(SearchResult result)
    {
        string text = $"In files '{result.Query.FileNames}': found {ResultMode.Counts(result)} "
            + $"in folder '{result.Query.FolderName}' ({result.Duration.TotalSeconds:0.0} s)";
        return result.Cancelled ? text + " — cancelled" : text;
    }

    /// <summary>
    /// Start of the matching lines pane header (the search string follows in quotes): "Lines with",
    /// "Lines without", "Files without" or "Files with".
    /// </summary>
    public static string LinesTitleKind(SearchQuery query)
    {
        ResultMode mode = ResultMode.Of(query);
        return mode.FilesWithoutMatch ? "Files without" : mode.FileNamesOnly ? "Files with"
            : mode.NonMatchingLines ? "Lines without" : "Lines with";
    }

    private static string NotFound(SearchResult result)
    {
        ResultMode mode = ResultMode.Of(result.Query);
        string what = mode.FilesWithoutMatch ? "Found no files without match for"
            : mode.NonMatchingLines ? "Found no lines without match for"
            : "Found no matches for";
        return $"<p class=\"NotFound\">{what} '{Encode(result.Query.SearchString)}'"
            + (result.Cancelled ? " (search cancelled)" : "") + "</p>";
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static string Page(string css, string background, string body, string? bodyClass = null, string? script = null) =>
        "<!DOCTYPE html>\n<html><head><meta charset=\"utf-8\"><style>\n"
        + $"body {{ background: {background}; }}\n"
        + css
        + "\n</style></head><body" + (bodyClass is null ? "" : $" class=\"{bodyClass}\"") + ">" + body
        + (script is null ? "" : "<script>\n" + script + "\n</script>")
        + "</body></html>\n";
}
