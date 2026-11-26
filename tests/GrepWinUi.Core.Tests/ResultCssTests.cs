using GrepWinUi.Core.Search;
using SaveAndLoad;

namespace GrepWinUi.Core.Tests;

/// <summary>Style sheets of the result panes: user file → defaults file → built-in text.</summary>
public sealed class ResultCssTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));
    private readonly StorageFolders _folders;

    public ResultCssTests()
    {
        _folders = new StorageFolders(Path.Combine(_root, "user"), Path.Combine(_root, "defaults"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Load_NoFiles_BuiltIn()
    {
        CssText css = ResultCss.Load(_folders, ResultCss.FileListFileName);

        Assert.Equal(ResultCss.FileList, css.Text);
        Assert.Equal(LoadSource.NewObject, css.Source);
        Assert.Null(css.Problem);
    }

    [Fact]
    public void Load_DefaultsFileOnly_DefaultsFile()
    {
        Write(_folders.DefaultsFolder, ResultCss.MatchingLinesFileName, ".a {}");

        CssText css = ResultCss.Load(_folders, ResultCss.MatchingLinesFileName);

        Assert.Equal(".a {}", css.Text);
        Assert.Equal(LoadSource.DefaultsFile, css.Source);
    }

    [Fact]
    public void Load_UserFile_WinsOverDefaultsFile()
    {
        Write(_folders.DefaultsFolder, ResultCss.FileListFileName, ".defaults {}");
        Write(_folders.UserFolder, ResultCss.FileListFileName, ".user {}");

        CssText css = ResultCss.Load(_folders, ResultCss.FileListFileName);

        Assert.Equal(".user {}", css.Text);
        Assert.Equal(LoadSource.UserFile, css.Source);
    }

    [Fact]
    public void Load_UserFileLocked_DefaultsFileWithProblem()
    {
        Write(_folders.DefaultsFolder, ResultCss.FileListFileName, ".defaults {}");
        string userFile = Write(_folders.UserFolder, ResultCss.FileListFileName, ".user {}");

        CssText css;
        using (new FileStream(userFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            css = ResultCss.Load(_folders, ResultCss.FileListFileName);
        }

        Assert.Equal(".defaults {}", css.Text);
        Assert.Equal(LoadSource.DefaultsFile, css.Source);
        Assert.Contains(userFile, css.Problem);
    }

    [Fact]
    public void BuiltIn_KnownNamesAndUnknownEmpty()
    {
        Assert.Equal(ResultCss.FileList, ResultCss.BuiltIn(ResultCss.FileListFileName));
        Assert.Equal(ResultCss.MatchingLines, ResultCss.BuiltIn(ResultCss.MatchingLinesFileName));
        Assert.Equal("", ResultCss.BuiltIn("Other.css"));
    }

    [Fact]
    public void BuiltIn_UsesClassesOfThePages()
    {
        Assert.Contains(".HeaderFoundFiles", ResultCss.FileList);
        Assert.Contains(".NotFound", ResultCss.FileList);
        Assert.Contains(".Problem", ResultCss.FileList);
        Assert.DoesNotContain("text-align: right", ResultCss.FileList);
        Assert.Contains("td.FileName div", ResultCss.FileList);
        Assert.Contains(".HeaderFoundToken", ResultCss.MatchingLines);
        Assert.Contains(".FileNameTitle", ResultCss.MatchingLines);
        Assert.Contains(".LineNumber", ResultCss.MatchingLines);
        Assert.Contains(".Token", ResultCss.MatchingLines);
    }

    [Theory]
    [InlineData(ResultCss.FileListFileName)]
    [InlineData(ResultCss.MatchingLinesFileName)]
    public void DefaultsFile_EqualsBuiltIn(string fileName)
    {
        string defaultsFile = Path.Combine(AppContext.BaseDirectory, "Defaults", fileName);

        Assert.Equal(Normalize(ResultCss.BuiltIn(fileName)), Normalize(File.ReadAllText(defaultsFile)));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();

    private static string Write(string folder, string fileName, string text)
    {
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, fileName);
        File.WriteAllText(path, text);
        return path;
    }
}
