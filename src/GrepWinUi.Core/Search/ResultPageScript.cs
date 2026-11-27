using System.Text.Json;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Clicks between the two result panes: the file list page reports a click on a file row to the program
/// (WebView2 <c>postMessage</c>), the program scrolls the matching lines pane to that file.
/// </summary>
public static class ResultPageScript
{
    /// <summary>Message kind of a click on a file row.</summary>
    public const string ClickKind = "click";

    /// <summary>
    /// Script of the file list page: a click on a row with <c>data-file</c> marks that row (CSS class
    /// <c>highlight</c>, as in the old program; any other row loses it) and posts
    /// <c>{"kind":"click","file":index}</c> to the program.
    /// </summary>
    public const string FileList = """
        document.addEventListener('click', function (event) {
            var row = event.target.closest('tr[data-file]');
            if (!row) { return; }
            document.querySelectorAll('tr.highlight').forEach(function (other) { other.classList.remove('highlight'); });
            row.classList.add('highlight');
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({ kind: 'click', file: Number(row.dataset.file) });
            }
        });
        """;

    /// <summary>Id of the element where the lines of file number <paramref name="index"/> start in the matching lines page.</summary>
    public static string FileId(int index) => $"file-{index}";

    /// <summary>Script for the matching lines pane: scrolls the start of file <paramref name="index"/> to the top (nothing if it isn't there).</summary>
    public static string ScrollToFile(int index) =>
        $"(function () {{ var e = document.getElementById('{FileId(index)}'); if (e) {{ e.scrollIntoView({{ block: 'start' }}); }} }})();";

    /// <summary>
    /// File index of a click message from the file list page (<c>WebMessageAsJson</c>), null for any other
    /// or broken message and for a negative index.
    /// </summary>
    public static int? ParseFileClick(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("kind", out JsonElement kind) && kind.ValueKind == JsonValueKind.String
                && kind.GetString() == ClickKind
                && root.TryGetProperty("file", out JsonElement file) && file.ValueKind == JsonValueKind.Number
                && file.TryGetInt32(out int index) && index >= 0
                ? index
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
