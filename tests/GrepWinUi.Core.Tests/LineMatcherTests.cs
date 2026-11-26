using System.Text.RegularExpressions;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Search string options: plain text and regular expression matchers.</summary>
public sealed class LineMatcherTests
{
    [Fact]
    public void PlainText_IgnoresCase_NotOverlapping()
    {
        LineMatcher matcher = LineMatcher.PlainText("aa");

        Assert.Equal([new Hit(0, 2), new Hit(2, 2), new Hit(6, 2)], matcher.Find("aAaa  AA"));
        Assert.Empty(matcher.Find("a"));
    }

    [Fact]
    public void PlainText_MatchCase()
    {
        LineMatcher matcher = LineMatcher.PlainText("Foo", matchCase: true);

        Assert.Equal([new Hit(8, 3)], matcher.Find("foo FOO Foo"));
    }

    [Fact]
    public void PlainText_SpecialCharactersAreNormal()
    {
        LineMatcher matcher = LineMatcher.PlainText("a.*b");

        Assert.Equal([new Hit(5, 4)], matcher.Find("axxb a.*b"));
    }

    [Theory]
    [InlineData("foo", "foo food _foo foo1 (foo) foo", new[] { 0, 20, 25 })]
    [InlineData(".NET", "x.NET .NET a.NETb", new[] { 6 })]
    [InlineData("C#", "C# C#x xC#", new[] { 0 })]
    [InlineData("aa", "aaa aa", new[] { 4 })]
    public void PlainText_WholeWords(string text, string line, int[] starts)
    {
        LineMatcher matcher = LineMatcher.PlainText(text, wholeWords: true);

        Assert.Equal(starts, matcher.Find(line).Select(hit => hit.Start));
    }

    [Fact]
    public void Regex_GroupsAndAlternatives()
    {
        LineMatcher matcher = LineMatcher.RegularExpression(@"(public|private) \w+");

        Assert.Equal([new Hit(0, 10), new Hit(12, 12)], matcher.Find("PUBLIC int; private void"));
    }

    [Fact]
    public void Regex_WholeWords_WholeAlternation()
    {
        LineMatcher matcher = LineMatcher.RegularExpression("a|b", wholeWords: true);

        Assert.Equal([new Hit(0, 1), new Hit(8, 1)], matcher.Find("a ab ba b"));
    }

    [Fact]
    public void Regex_MatchCase()
    {
        Assert.Single(LineMatcher.RegularExpression("Foo", matchCase: true).Find("foo Foo"));
        Assert.Equal(2, LineMatcher.RegularExpression("Foo").Find("foo Foo").Count);
    }

    [Fact]
    public void Regex_EmptyMatch_LengthZeroHitAndEnds()
    {
        Assert.Equal([new Hit(0, 0)], LineMatcher.RegularExpression("^$").Find(""));
        Assert.Empty(LineMatcher.RegularExpression("^$").Find("x"));
        Assert.Equal(3, LineMatcher.RegularExpression("x*").Find("ab").Count);
    }

    [Fact]
    public void Regex_Invalid_ThrowsAndRegexErrorTellsWhy()
    {
        Assert.ThrowsAny<ArgumentException>(() => LineMatcher.RegularExpression("(abc"));
        Assert.NotNull(LineMatcher.RegexError("(abc"));
        Assert.NotNull(LineMatcher.RegexError(""));
        Assert.Null(LineMatcher.RegexError(@"\d+"));
    }

    [Fact]
    public void Regex_TooSlow_TimeoutException()
    {
        LineMatcher matcher = LineMatcher.RegularExpression("(a+)+$");

        Assert.Throws<RegexMatchTimeoutException>(() => matcher.Find(new string('a', 40) + "!"));
    }

    [Theory]
    [InlineData("foo", false, false, "Foo foo food x.foo")]
    [InlineData("foo", true, false, "Foo foo food x.foo")]
    [InlineData("foo", false, true, "Foo foo food x.foo _foo")]
    [InlineData("a.*b", false, true, "a.*b xa.*b a.*b_ (a.*b)")]
    [InlineData(".NET", false, true, "x.NET .NET a.NETb")]
    [InlineData("C#", true, true, "C# c# C#x xC#")]
    public void PlainText_SameHitsAsEscapedRegex(string text, bool matchCase, bool wholeWords, string line)
    {
        IReadOnlyList<Hit> plain = LineMatcher.PlainText(text, matchCase, wholeWords).Find(line);
        IReadOnlyList<Hit> regex = LineMatcher.RegularExpression(Regex.Escape(text), matchCase, wholeWords).Find(line);

        Assert.Equal(regex, plain);
    }

    [Theory]
    [InlineData("foo", false, "  foo  ")]
    [InlineData("foo", false, "FOO")]
    [InlineData("foo", true, "FOO")]
    [InlineData("foo", false, "foo bar")]
    [InlineData("a.b", false, "	a.b ")]
    [InlineData("a.b", false, "axb")]
    public void WholeLine_PlainSameHitsAsEscapedRegex(string text, bool matchCase, string line)
    {
        IReadOnlyList<Hit> plain = LineMatcher.PlainText(text, matchCase, wholeLine: true).Find(line);
        IReadOnlyList<Hit> regex = LineMatcher.RegularExpression(Regex.Escape(text), matchCase, wholeLine: true).Find(line);

        Assert.Equal(regex, plain);
    }

    [Fact]
    public void WholeLine_HitIsLineWithoutBlanksAround()
    {
        Assert.Equal([new Hit(2, 3)], LineMatcher.PlainText("foo", wholeLine: true).Find("  foo "));
        Assert.Empty(LineMatcher.PlainText("foo", wholeLine: true).Find("foo bar"));
        Assert.Equal([new Hit(1, 6)], LineMatcher.RegularExpression("a|abcdef", wholeLine: true).Find(" abcdef"));
    }

    [Fact]
    public void Create_UsesQueryOptions()
    {
        SearchQuery query = new() { SearchString = "a.c", RegularExpression = true };
        Assert.Single(LineMatcher.Create(query).Find("abc"));

        query.RegularExpression = false;
        Assert.Empty(LineMatcher.Create(query).Find("abc"));
    }
}
