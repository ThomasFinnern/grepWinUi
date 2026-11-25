namespace GrepWinUi.App.Results;

/// <summary>Fixed dummy HTML for the two result panes until real results are built (later in Core).</summary>
internal static class DummyResultHtml
{
    /// <summary>Form background color (e.g. #F0F0F0), so the panes look like the rest of the window.</summary>
    private static string Background
    {
        get
        {
            Color color = SystemColors.Control;
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    private static string Style => $$"""
        <style>
          body { font-family: Verdana, sans-serif; font-size: 10pt; margin: 8px; background: {{Background}}; }
          .head { background: #4a86c8; color: white; font-weight: bold; padding: 2px 6px; }
          .head .big { font-size: 13pt; }
          hr { border: 0; border-top: 2px solid #999; }
          table { border-collapse: collapse; }
          th { text-align: left; padding-right: 24px; }
          td { padding-right: 24px; white-space: nowrap; }
          .num { text-align: right; }
          .line { font-family: Consolas, monospace; white-space: pre; margin-left: 16px; }
          .hit { color: green; font-weight: bold; }
        </style>
        """;

    /// <summary>Dummy file list pane.</summary>
    public static string FileList => $$"""
        <html><head>{{Style}}</head><body>
        <div class="head"><span class="big">Results for searched string: '[public|protected|private]\s+function '</span><br>
        in files: '*controller.php'. Found 154 matches in 48 files from 57 files searched in folders: 'e:\wamp64\www\api_6x\api\components'.</div>
        <hr>
        <table>
          <tr><th>Filename</th><th>Folder</th><th>Matches</th><th>Size</th><th>Date/Time</th></tr>
          <tr><td>BannersController.php</td><td>e:\wamp64\www\api_6x\api\components\com_banners\src\Controller</td><td>1</td><td class="num">1427</td><td>21.04.2026 14:51:35</td></tr>
          <tr><td>CategoriesController.php</td><td>e:\wamp64\www\api_6x\api\components\com_categories\src\Controller</td><td>5</td><td class="num">6812</td><td>21.04.2026 14:51:35</td></tr>
          <tr><td>ApplicationController.php</td><td>e:\wamp64\www\api_6x\api\components\com_config\src\Controller</td><td>3</td><td class="num">4793</td><td>21.04.2026 14:51:35</td></tr>
        </table>
        </body></html>
        """;

    /// <summary>Dummy matching lines pane.</summary>
    public static string MatchingLines => $$"""
        <html><head>{{Style}}</head><body>
        <div class="head"><span class="big">Token found inside files</span></div>
        <hr>
        <div class="line">public <span class="hit">function</span> displayList()</div>
        <div class="line">protected <span class="hit">function</span> preprocessSaveData(array $data): array</div>
        <div class="line">protected <span class="hit">function</span> save($recordKey = null)</div>
        <div class="line">private <span class="hit">function</span> getExtensionFromInput()</div>
        </body></html>
        """;
}
