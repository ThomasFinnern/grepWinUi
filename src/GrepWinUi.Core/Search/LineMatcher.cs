namespace GrepWinUi.Core.Search;

/// <summary>
/// Finds the hits of the search string in a line. For now plain text, case ignored
/// (Match case, Whole words only and Regular expression follow in a later step).
/// </summary>
public sealed class LineMatcher
{
    private readonly string _text;

    private LineMatcher(string text) => _text = text;

    /// <summary>Plain text search ignoring case.</summary>
    /// <exception cref="ArgumentException">The text is empty.</exception>
    public static LineMatcher PlainText(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        return new LineMatcher(text);
    }

    /// <summary>All hits in the line, left to right, not overlapping.</summary>
    public IReadOnlyList<Hit> Find(string line)
    {
        List<Hit> hits = [];
        int start = 0;
        while (start <= line.Length - _text.Length)
        {
            int index = line.IndexOf(_text, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                break;
            }
            hits.Add(new Hit(index, _text.Length));
            start = index + _text.Length;
        }
        return hits;
    }
}
