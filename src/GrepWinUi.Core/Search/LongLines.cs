namespace GrepWinUi.Core.Search;

/// <summary>
/// Cuts very long lines (binary files, minified files) so the result pages stay small: a matching line keeps
/// a window of <see cref="MaxLength"/> characters around its first hit, a kept line its first
/// <see cref="MaxLength"/> characters; a cut end is marked with '…'.
/// </summary>
public static class LongLines
{
    /// <summary>Longest line text kept in a result.</summary>
    public const int MaxLength = 2000;

    /// <summary>Characters kept before the first hit when a matching line is cut.</summary>
    public const int KeepBeforeHit = 200;

    /// <summary>Marks a cut end of a line.</summary>
    public const string CutMark = "…";

    /// <summary>
    /// The line itself when it is short enough; else the window around its first hit, hits inside the window
    /// moved to the new text, hits outside dropped.
    /// </summary>
    public static LineMatch Cut(LineMatch line)
    {
        string text = line.Text;
        if (text.Length <= MaxLength)
        {
            return line;
        }

        int firstHit = line.Hits.Count > 0 ? line.Hits[0].Start : 0;
        int start = Math.Clamp(firstHit - KeepBeforeHit, 0, text.Length - MaxLength);
        int end = start + MaxLength;
        string prefix = start > 0 ? CutMark : "";
        string suffix = end < text.Length ? CutMark : "";
        int shift = prefix.Length - start;
        List<Hit> hits = [.. line.Hits
            .Where(hit => hit.Start >= start && hit.Start + hit.Length <= end)
            .Select(hit => hit with { Start = hit.Start + shift })];
        return line with { Text = prefix + text[start..end] + suffix, Hits = hits };
    }

    /// <summary>The first <see cref="MaxLength"/> characters of a kept line, '…' at the end when cut.</summary>
    public static string Cut(string text) => text.Length <= MaxLength ? text : text[..MaxLength] + CutMark;
}
