using System.Globalization;
using System.Text;

namespace GrepWinUi.Core.Search;

/// <summary>
/// Text or binary file and the encoding to read it with, decided from its first bytes (<see cref="SampleSize"/>):
/// byte order mark (UTF-8, UTF-16 LE/BE, UTF-32 LE/BE) → UTF-16 without byte order mark (most character pairs
/// have a 0 byte on the same side) → a 0 byte means binary → valid UTF-8 → else the PC's ANSI code page.
/// A binary file is read with Latin-1 (each byte one character).
/// </summary>
/// <param name="IsBinary">True for a binary file.</param>
/// <param name="Encoding">Encoding to read the file with.</param>
public sealed record FileKind(bool IsBinary, Encoding Encoding)
{
    /// <summary>Number of bytes at the start of a file used to decide.</summary>
    public const int SampleSize = 8192;

    /// <summary>ANSI code page of the PC (e.g. Windows-1252), Latin-1 if it is not available.</summary>
    public static Encoding Ansi { get; } = CreateAnsi();

    /// <summary>Decides from the first bytes of a file (an empty file is UTF-8 text).</summary>
    public static FileKind Detect(ReadOnlySpan<byte> sample)
    {
        if (sample.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
        {
            return Text(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        if (sample.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE, 0x00, 0x00]))
        {
            return Text(new UTF32Encoding(bigEndian: false, byteOrderMark: true));
        }
        if (sample.StartsWith((ReadOnlySpan<byte>)[0x00, 0x00, 0xFE, 0xFF]))
        {
            return Text(new UTF32Encoding(bigEndian: true, byteOrderMark: true));
        }
        if (sample.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE]))
        {
            return Text(Encoding.Unicode);
        }
        if (sample.StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF]))
        {
            return Text(Encoding.BigEndianUnicode);
        }
        if (Utf16WithoutBom(sample) is Encoding utf16)
        {
            return Text(utf16);
        }
        if (sample.Contains((byte)0))
        {
            return new FileKind(IsBinary: true, Encoding.Latin1);
        }
        return Text(IsValidUtf8(sample) ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) : Ansi);
    }

    /// <summary>Reads the first <see cref="SampleSize"/> bytes of a file and decides.</summary>
    /// <exception cref="IOException">The file can't be read.</exception>
    /// <exception cref="UnauthorizedAccessException">No access to the file.</exception>
    public static FileKind DetectFile(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] buffer = new byte[SampleSize];
        int length = stream.ReadAtLeast(buffer, SampleSize, throwOnEndOfStream: false);
        return Detect(buffer.AsSpan(0, length));
    }

    /// <summary>
    /// Text of a binary file line for searching and showing: control characters (except tab) become '·',
    /// so every byte stays one character and hit positions stay right.
    /// </summary>
    public static string ShowControlCharacters(string line) =>
        string.Create(line.Length, line, (chars, source) =>
        {
            for (int index = 0; index < source.Length; index++)
            {
                char c = source[index];
                chars[index] = c != '\t' && (c < ' ' || (c >= '\u007F' && c <= '\u009F')) ? '·' : c;
            }
        });

    private static FileKind Text(Encoding encoding) => new(IsBinary: false, encoding);

    /// <summary>UTF-16 LE / BE when at least 70 % of the character pairs have a 0 byte on one side and at most 10 % on the other.</summary>
    private static Encoding? Utf16WithoutBom(ReadOnlySpan<byte> sample)
    {
        int pairs = sample.Length / 2;
        if (pairs < 2)
        {
            return null;
        }
        int zeroEven = 0;
        int zeroOdd = 0;
        for (int index = 0; index + 1 < sample.Length; index += 2)
        {
            zeroEven += sample[index] == 0 ? 1 : 0;
            zeroOdd += sample[index + 1] == 0 ? 1 : 0;
        }
        if (zeroOdd >= pairs * 0.7 && zeroEven <= pairs * 0.1)
        {
            return Encoding.Unicode;
        }
        if (zeroEven >= pairs * 0.7 && zeroOdd <= pairs * 0.1)
        {
            return Encoding.BigEndianUnicode;
        }
        return null;
    }

    /// <summary>True when the bytes are valid UTF-8 (a character cut off at the end of the sample is allowed).</summary>
    private static bool IsValidUtf8(ReadOnlySpan<byte> sample)
    {
        Decoder decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
        try
        {
            decoder.GetCharCount(sample, flush: false);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static Encoding CreateAnsi()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            int codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
            return codePage > 0 ? Encoding.GetEncoding(codePage) : Encoding.Latin1;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.Latin1;
        }
    }
}
