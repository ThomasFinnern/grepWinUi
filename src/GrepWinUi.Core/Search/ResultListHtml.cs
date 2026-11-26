using System.Net;
using System.Text;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Simple HTML pages for the two result panes: the file list and the matching lines, as plain lists.
/// Interim view until the result layout with CSS files is built (roadmap step 18).
/// All texts are HTML encoded. Very long results are cut (WebView2 shows at most about 2 MB of HTML).
/// </summary>
public static class ResultListHtml
{
    /// <summary>Most matching lines written to the page.</summary>
    public const int MaxLines = 5000;

    /// <summary>Page shown before the first search.</summary>
    public static string Hint(string text, string background) => Page(background, $"<p>{Encode(text)}</p>");

    /// <summary>File list pane: a summary line, then one line per file with its number of hits.</summary>
    public static string FileList(SearchResult result, string background)
    {
        StringBuilder body = new();
        body.Append("<p class=\"head\">").Append(Encode(Summary(result))).Append("</p>");
        AppendProblems(body, result);
        body.Append("<ol>");
        foreach (FileResult file in result.Files)
        {
            int hits = file.Lines.Sum(line => line.Hits.Count);
            body.Append("<li>").Append(Encode(file.Path)).Append($" <span class=\"count\">({hits})</span></li>");
        }
        body.Append("</ol>");
        return Page(background, body.ToString());
    }

    /// <summary>Matching lines pane: per file its path, then "line number: text" with marked hits.</summary>
    public static string MatchingLines(SearchResult result, string background)
    {
        StringBuilder body = new();
        int written = 0;
        foreach (FileResult file in result.Files)
        {
            body.Append("<p class=\"file\">").Append(Encode(file.Path)).Append("</p>");
            foreach (LineMatch line in file.Lines)
            {
                if (written++ == MaxLines)
                {
                    body.Append($"<p class=\"head\">… only the first {MaxLines} lines are shown.</p>");
                    return Page(background, body.ToString());
                }
                body.Append("<div class=\"line\"><span class=\"num\">").Append(line.LineNumber).Append(":</span> ")
                    .Append(MarkHits(line)).Append("</div>");
            }
        }
        return Page(background, body.ToString());
    }

    /// <summary>One line text with every hit wrapped in &lt;mark&gt;.</summary>
    public static string MarkHits(LineMatch line)
    {
        StringBuilder text = new();
        int position = 0;
        foreach (Hit hit in line.Hits)
        {
            text.Append(Encode(line.Text[position..hit.Start]))
                .Append("<mark>").Append(Encode(line.Text.Substring(hit.Start, hit.Length))).Append("</mark>");
            position = hit.Start + hit.Length;
        }
        return text.Append(Encode(line.Text[position..])).ToString();
    }

    /// <summary>E.g. "'function' in '*.cs': 12 hits in 3 of 57 files (0.4 s)".</summary>
    public static string Summary(SearchResult result)
    {
        string text = $"'{result.Query.SearchString}' in '{result.Query.FileNames}': {result.MatchCount} hits in "
            + $"{result.Files.Count} of {result.FilesSearched} files ({result.Duration.TotalSeconds:0.0} s)";
        return result.Cancelled ? text + " — cancelled" : text;
    }

    private static void AppendProblems(StringBuilder body, SearchResult result)
    {
        foreach (string problem in result.Problems)
        {
            body.Append("<p class=\"problem\">").Append(Encode(problem)).Append("</p>");
        }
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static string Page(string background, string body) => $$"""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8"><style>
          body { font-family: Verdana, sans-serif; font-size: 10pt; margin: 8px; background: {{background}}; }
          .head { font-weight: bold; }
          .problem { color: #b00000; }
          .count { color: #666; }
          .file { font-weight: bold; margin: 10px 0 2px 0; }
          .line { font-family: Consolas, monospace; white-space: pre; margin-left: 16px; }
          .num { color: #666; }
          mark { background: #c8f0c8; color: inherit; font-weight: bold; }
        </style></head><body>{{body}}</body></html>
        """;
}
