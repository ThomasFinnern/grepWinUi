using System.Net;
using System.Text;
using GrepWinUi.Core.AppData.Config;

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
    /// File name / Folder / Matches, or "Found no matches".
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

        body.Append("<table><tr><th class=\"FileName\">File name</th><th class=\"Folder\">Folder</th>"
            + "<th class=\"Matches\">Matches</th></tr>");
        foreach (FileResult file in result.Files)
        {
            int hits = file.Lines.Sum(line => line.Hits.Count);
            string name = Encode(Path.GetFileName(file.Path));
            body.Append("<tr><td class=\"FileName\"><div title=\"").Append(name).Append("\">").Append(name)
                .Append("</div></td><td class=\"Folder\">").Append(Encode(Path.GetDirectoryName(file.Path) ?? ""))
                .Append("</td><td class=\"Matches\">").Append(hits).Append("</td></tr>");
        }
        body.Append("</table>");
        return Page(css, background, body.ToString());
    }

    /// <summary>
    /// Matching lines pane: header, then per file its path (<c>FileNameTitle</c>) and its lines
    /// (<c>LineNumber</c> + text with <c>Token</c> hits), or "Found no matches". Around each matching line
    /// up to <see cref="ResultViewOptions.ShowLinesBefore"/> / <see cref="ResultViewOptions.ShowLinesAfter"/>
    /// kept lines (<c>Line ContextLine</c>), each line once; lines after a match stop at the next match.
    /// Groups of lines that don't follow each other are parted by <c>Gap</c> (only when lines before /
    /// after are shown).
    /// </summary>
    public static string MatchingLines(SearchResult result, ResultViewOptions view, string css, string background)
    {
        StringBuilder body = new();
        body.Append("<div class=\"HeaderFoundToken\"><h1>Lines with '")
            .Append(Encode(result.Query.SearchString)).Append("'</h1></div>");
        if (result.Files.Count == 0)
        {
            body.Append(NotFound(result));
            return Page(css, background, body.ToString());
        }

        int written = 0;
        foreach (FileResult file in result.Files)
        {
            body.Append("<div class=\"FileNameTitle\">").Append(Encode(file.Path)).Append("</div>");
            foreach (ShownLine shown in ShownLines(file, Math.Max(0, view.ShowLinesBefore), Math.Max(0, view.ShowLinesAfter)))
            {
                if (shown.GapBefore)
                {
                    body.Append("<div class=\"Gap\"></div>");
                }
                if (written++ == MaxLines)
                {
                    body.Append($"<p class=\"NotFound\">… only the first {MaxLines} lines are shown.</p>");
                    return Page(css, background, body.ToString());
                }
                body.Append(shown.Match is LineMatch match
                    ? $"<div class=\"Line\"><span class=\"LineNumber\">{LineNumber(shown.LineNumber)}: </span>{MarkHits(match)}</div>"
                    : $"<div class=\"Line ContextLine\"><span class=\"LineNumber\">{LineNumber(shown.LineNumber)}: </span>{Encode(shown.Text)}</div>");
            }
        }
        return Page(css, background, body.ToString());
    }

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
    /// E.g. "In files '*.cs': found 12 matches in 3 files from 57 files searched in folder 'c:\src' (0.4 s)";
    /// " — cancelled" at the end when the search was stopped.
    /// </summary>
    public static string Summary(SearchResult result)
    {
        string text = $"In files '{result.Query.FileNames}': found {result.MatchCount} matches in {result.Files.Count} files "
            + $"from {result.FilesSearched} files searched in folder '{result.Query.FolderName}' ({result.Duration.TotalSeconds:0.0} s)";
        return result.Cancelled ? text + " — cancelled" : text;
    }

    private static string NotFound(SearchResult result) =>
        $"<p class=\"NotFound\">Found no matches for '{Encode(result.Query.SearchString)}'"
        + (result.Cancelled ? " (search cancelled)" : "") + "</p>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static string Page(string css, string background, string body) =>
        "<!DOCTYPE html>\n<html><head><meta charset=\"utf-8\"><style>\n"
        + $"body {{ background: {background}; }}\n"
        + css
        + "\n</style></head><body>" + body + "</body></html>\n";
}
