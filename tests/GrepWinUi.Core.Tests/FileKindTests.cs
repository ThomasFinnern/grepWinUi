using System.Text;
using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Text / binary detection, encodings, long lines (temp folders only).</summary>
public sealed class FileKindTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public FileKindTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Detect_ByteOrderMarks()
    {
        Assert.Equal(Encoding.UTF8.CodePage, FileKind.Detect([0xEF, 0xBB, 0xBF, 0x41]).Encoding.CodePage);
        Assert.Equal(Encoding.Unicode.CodePage, FileKind.Detect([0xFF, 0xFE, 0x41, 0x00]).Encoding.CodePage);
        Assert.Equal(Encoding.BigEndianUnicode.CodePage, FileKind.Detect([0xFE, 0xFF, 0x00, 0x41]).Encoding.CodePage);
        Assert.Equal(Encoding.UTF32.CodePage, FileKind.Detect([0xFF, 0xFE, 0x00, 0x00, 0x41, 0, 0, 0]).Encoding.CodePage);
        Assert.Equal(12001, FileKind.Detect([0x00, 0x00, 0xFE, 0xFF, 0, 0, 0, 0x41]).Encoding.CodePage);
    }

    [Fact]
    public void Detect_Utf16WithoutBom()
    {
        FileKind little = FileKind.Detect(Encoding.Unicode.GetBytes("function grüße"));
        FileKind big = FileKind.Detect(Encoding.BigEndianUnicode.GetBytes("function grüße"));

        Assert.False(little.IsBinary);
        Assert.Equal(Encoding.Unicode.CodePage, little.Encoding.CodePage);
        Assert.Equal(Encoding.BigEndianUnicode.CodePage, big.Encoding.CodePage);
    }

    [Fact]
    public void Detect_ZeroByteIsBinary()
    {
        FileKind kind = FileKind.Detect([0x4D, 0x5A, 0x90, 0x00, 0x03, 0x41, 0x42, 0x43, 0x44, 0x45]);

        Assert.True(kind.IsBinary);
        Assert.Equal(Encoding.Latin1.CodePage, kind.Encoding.CodePage);
    }

    [Fact]
    public void Detect_Utf8AnsiEmpty()
    {
        Assert.Equal(Encoding.UTF8.CodePage, FileKind.Detect(Encoding.UTF8.GetBytes("grüße")).Encoding.CodePage);
        Assert.Equal(FileKind.Ansi.CodePage, FileKind.Detect([0x67, 0x72, 0xFC, 0xDF, 0x65]).Encoding.CodePage);
        Assert.Equal(Encoding.UTF8.CodePage, FileKind.Detect([0x67, 0xC3]).Encoding.CodePage);
        Assert.False(FileKind.Detect([]).IsBinary);
    }

    [Fact]
    public void ShowControlCharacters_KeepsLengthAndTab()
    {
        Assert.Equal("a·b\tc··ä", FileKind.ShowControlCharacters("a\u0001b\tc\u007F\u0085ä"));
    }

    [Fact]
    public void Search_AnsiAndUtf16FilesReadRight()
    {
        File.WriteAllBytes(Path.Combine(_root, "ansi.txt"), FileKind.Ansi.GetBytes("Grüße aus Köln"));
        File.WriteAllBytes(Path.Combine(_root, "wide.txt"), Encoding.Unicode.GetBytes("Grüße aus Bonn"));

        SearchResult result = Run(Query("grüße"));

        Assert.Equal(["Grüße aus Köln", "Grüße aus Bonn"], result.Files.Select(file => file.Lines[0].Text));
    }

    [Fact]
    public void Search_BinaryFiles_SearchedSkippedOrOnly()
    {
        File.WriteAllBytes(Path.Combine(_root, "app.bin"), [.. Encoding.ASCII.GetBytes("MZ"), 0, 1, .. Encoding.ASCII.GetBytes("foo\nbar")]);
        File.WriteAllText(Path.Combine(_root, "text.txt"), "foo");

        SearchResult all = Run(Query("foo"));
        Assert.Equal(["app.bin", "text.txt"], Names(all));
        Assert.Equal("MZ··foo", all.Files[0].Lines[0].Text);
        Assert.Equal([new Hit(4, 3)], all.Files[0].Lines[0].Hits);

        SearchQuery skip = Query("foo");
        skip.SkipBinaryFiles = true;
        Assert.Equal(["text.txt"], Names(Run(skip)));
        Assert.Equal(2, Run(skip).FilesSearched);

        SearchQuery only = Query("foo");
        only.OnlyBinaryFiles = true;
        Assert.Equal(["app.bin"], Names(Run(only)));
    }

    [Fact]
    public void StartProblems_BothBinaryOptions()
    {
        SearchQuery query = Query("foo");
        Assert.Empty(FileFilter.StartProblems(query));

        query.SkipBinaryFiles = true;
        query.OnlyBinaryFiles = true;
        Assert.Contains("together search no file", Assert.Single(FileFilter.StartProblems(query)));
    }

    [Fact]
    public void LongLines_ShortLineUnchanged()
    {
        LineMatch line = new(1, "x foo", [new Hit(2, 3)]);

        Assert.Same(line, LongLines.Cut(line));
    }

    [Fact]
    public void LongLines_WindowAroundFirstHitHitsMoved()
    {
        string text = new string('a', 5000) + "foo" + new string('b', 100) + "foo" + new string('c', 5000);
        LineMatch line = new(1, text, [new Hit(5000, 3), new Hit(5103, 3), new Hit(9000, 3)]);

        LineMatch cut = LongLines.Cut(line);

        Assert.Equal(LongLines.MaxLength + 2, cut.Text.Length);
        Assert.StartsWith("…", cut.Text);
        Assert.EndsWith("…", cut.Text);
        Assert.Equal([new Hit(LongLines.KeepBeforeHit + 1, 3), new Hit(LongLines.KeepBeforeHit + 104, 3)], cut.Hits);
        Assert.Equal("foo", cut.Text.Substring(cut.Hits[0].Start, 3));
    }

    [Fact]
    public void LongLines_HitAtStartAndKeptLine()
    {
        string text = "foo" + new string('x', 3000);

        LineMatch cut = LongLines.Cut(new LineMatch(1, text, [new Hit(0, 3)]));

        Assert.False(cut.Text.StartsWith('…'));
        Assert.Equal([new Hit(0, 3)], cut.Hits);
        Assert.Equal(LongLines.MaxLength + 1, LongLines.Cut(text).Length);
    }

    [Fact]
    public void Search_LongLineCutInResult()
    {
        File.WriteAllText(Path.Combine(_root, "min.js"), new string('x', 10000) + "foo" + new string('y', 10000));

        LineMatch line = Run(Query("foo")).Files[0].Lines[0];

        Assert.True(line.Text.Length <= LongLines.MaxLength + 2);
        Assert.Equal("foo", line.Text.Substring(line.Hits[0].Start, 3));
    }

    private SearchQuery Query(string text) => new() { SearchString = text, FileNames = "*.*", FolderName = $"\"{_root}\"" };

    private static SearchResult Run(SearchQuery query) => SearchEngine.Run(query, null, CancellationToken.None);

    private static List<string> Names(SearchResult result) => [.. result.Files.Select(file => Path.GetFileName(file.Path))];
}
