using System.Text.RegularExpressions;
using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Finds the hits of the search string in a line. Two kinds: plain text (<c>IndexOf</c>, the default)
/// and regular expression (only with "Regular expression"). Both know "Match case" (else upper / lower
/// case is ignored), "Whole words only" (no letter, digit or '_' directly before or after a hit) and
/// "Match whole line" (the search string covers the whole line except leading / trailing blanks; the hit
/// is the line without them; whole words is then not needed).
/// </summary>
public abstract class LineMatcher
{
    /// <summary>Longest time a regular expression may take for one line.</summary>
    public static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Matcher for the search string options of <paramref name="query"/>.</summary>
    /// <exception cref="ArgumentException">The search string is empty or not a valid regular expression.</exception>
    public static LineMatcher Create(SearchQuery query) => query.RegularExpression
        ? RegularExpression(query.SearchString, query.MatchCase, query.WholeWordsOnly, query.MatchWholeLine)
        : PlainText(query.SearchString, query.MatchCase, query.WholeWordsOnly, query.MatchWholeLine);

    /// <summary>Plain text search: special characters like '.' or '*' are normal characters.</summary>
    /// <exception cref="ArgumentException">The text is empty.</exception>
    public static LineMatcher PlainText(string text, bool matchCase = false, bool wholeWords = false, bool wholeLine = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        return wholeLine ? new WholeLineMatcher(text, matchCase) : new PlainTextMatcher(text, matchCase, wholeWords);
    }

    /// <summary>
    /// Regular expression search (.NET syntax, case rules the same on every PC). A match of length 0
    /// (e.g. <c>^$</c> for empty lines) is a hit of length 0. Each line may take at most <see cref="RegexTimeout"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The pattern is empty or not valid.</exception>
    public static LineMatcher RegularExpression(string pattern, bool matchCase = false, bool wholeWords = false, bool wholeLine = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        string full = wholeLine ? $@"(?<=^\s*)(?:{pattern})(?=\s*$)"
            : wholeWords ? $@"(?<!\w)(?:{pattern})(?!\w)"
            : pattern;
        RegexOptions options = RegexOptions.CultureInvariant | (matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
        return new RegexMatcher(new Regex(full, options, RegexTimeout));
    }

    /// <summary>Problem text of a regular expression, e.g. for the search dialog; null when it is valid.</summary>
    public static string? RegexError(string pattern)
    {
        if (pattern.Length == 0)
        {
            return "The search string is empty.";
        }
        try
        {
            _ = new Regex(pattern);
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }

    /// <summary>All hits in the line, left to right, not overlapping.</summary>
    /// <exception cref="RegexMatchTimeoutException">A regular expression took longer than <see cref="RegexTimeout"/>.</exception>
    public abstract IReadOnlyList<Hit> Find(string line);

    /// <summary>Characters that make up words (like <c>\w</c> in regular expressions).</summary>
    public static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private sealed class PlainTextMatcher(string text, bool matchCase, bool wholeWords) : LineMatcher
    {
        private readonly StringComparison _comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        public override IReadOnlyList<Hit> Find(string line)
        {
            List<Hit> hits = [];
            int start = 0;
            while (start <= line.Length - text.Length)
            {
                int index = line.IndexOf(text, start, _comparison);
                if (index < 0)
                {
                    break;
                }
                if (wholeWords && !IsWholeWord(line, index, text.Length))
                {
                    start = index + 1;
                    continue;
                }
                hits.Add(new Hit(index, text.Length));
                start = index + text.Length;
            }
            return hits;
        }

        private static bool IsWholeWord(string line, int index, int length) =>
            (index == 0 || !IsWordChar(line[index - 1]))
            && (index + length == line.Length || !IsWordChar(line[index + length]));
    }

    private sealed class WholeLineMatcher(string text, bool matchCase) : LineMatcher
    {
        private readonly StringComparison _comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        public override IReadOnlyList<Hit> Find(string line)
        {
            ReadOnlySpan<char> trimmed = line.AsSpan().Trim();
            if (!trimmed.Equals(text, _comparison))
            {
                return [];
            }
            int start = line.Length - line.AsSpan().TrimStart().Length;
            return [new Hit(start, trimmed.Length)];
        }
    }

    private sealed class RegexMatcher(Regex regex) : LineMatcher
    {
        public override IReadOnlyList<Hit> Find(string line)
        {
            List<Hit> hits = [];
            // Matches moves on by one character after a match of length 0, so this always ends.
            foreach (Match match in regex.Matches(line))
            {
                hits.Add(new Hit(match.Index, match.Length));
            }
            return hits;
        }
    }
}
