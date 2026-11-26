using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>File and folder filters, several folders (temp folders only).</summary>
public sealed class FileFilterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public FileFilterTests()
    {
        foreach (string file in new[]
        {
            "a.cs", "b.md", "readme.txt", "app.exe",
            @"src\x.cs", @"src\tests\t.cs", @"src\bin\out.cs", @".git\g.cs",
            @"my folder\m.cs", @"other\o.cs",
        })
        {
            string path = Path.Combine(_root, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "x");
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void FolderList_BlanksQuotesDuplicates()
    {
        Assert.Equal([@"c:\a", @"d:\b"], FolderList.Split(@" c:\a   d:\b ", _ => false));
        Assert.Equal([@"c:\my folder", @"d:\b"], FolderList.Split(@"""c:\my folder"" d:\b", _ => false));
        Assert.Equal([@"c:\a", @"d:\b"], FolderList.Split(@"c:\a""d:\b""", _ => false));
        Assert.Equal([@"c:\a"], FolderList.Split(@"c:\a C:\A", _ => false));
        Assert.Equal([@"c:\open end"], FolderList.Split(@"""c:\open end", _ => false));
        Assert.Empty(FolderList.Split("  ", _ => true));
    }

    [Fact]
    public void FolderList_WholeTextExistingFolder_OneFolderWithoutQuotes()
    {
        Assert.Equal([@"c:\my folder"], FolderList.Split(@"c:\my folder", folder => folder == @"c:\my folder"));
        Assert.Equal([@"c:\my", "folder"], FolderList.Split(@"c:\my folder", _ => false));
    }

    [Fact]
    public void ExtensionPatterns_AllFormsBecomeWildcards()
    {
        Assert.Equal(["*.exe", "*.dll", "*.obj", "a?.*"], FileFilter.ExtensionPatterns("*.exe .dll obj a?.*"));
    }

    [Fact]
    public void FileNamesAsRegex_PartOfNameIgnoringCase()
    {
        SearchQuery query = Query();
        query.FileNames = @"\.(CS|md)$";
        query.FileNamesAsRegex = true;

        Assert.Equal(["a.cs", "b.md", @".git\g.cs", @"my folder\m.cs", @"other\o.cs", @"src\x.cs", @"src\bin\out.cs", @"src\tests\t.cs"], Find(query));
    }

    [Fact]
    public void SkipByExtension_SkipsListedExtensions()
    {
        SearchQuery query = Query();
        query.IncludeSubfolders = false;
        query.SkipByExtension = true;
        query.SkippedExtensions = "exe .MD";

        Assert.Equal(["a.cs", "readme.txt"], Find(query));
    }

    [Fact]
    public void SkipFolders_NamesAndWildcardsWithTheirSubfolders()
    {
        SearchQuery query = Query("*.cs");
        query.SkipFolders = true;
        query.SkippedFolders = "BIN .git my* src";

        Assert.Equal(["a.cs", @"other\o.cs"], Find(query));

        query.SkippedFolders = "t*";
        Assert.DoesNotContain(@"src\tests\t.cs", Find(query));
        Assert.Contains(@"src\bin\out.cs", Find(query));
    }

    [Fact]
    public void FolderNamesAsRegex_PathName_PathBelowStartFolder()
    {
        SearchQuery query = Query("*.cs");
        query.FolderNamesAsRegex = true;
        query.RegularExpressionOn = RegexTarget.PathName;
        query.FolderNamePattern = @"\\tests$";

        Assert.Equal([@"src\tests\t.cs"], Find(query));

        query.FolderNamePattern = "^$";
        Assert.Equal(["a.cs"], Find(query));
    }

    [Fact]
    public void FolderNamesAsRegex_PathAndFileName()
    {
        SearchQuery query = Query("*.*");
        query.FolderNamesAsRegex = true;
        query.RegularExpressionOn = RegexTarget.PathAndFileName;
        query.FolderNamePattern = @"^SRC\\[^\\]+\.cs$";

        Assert.Equal([@"src\x.cs"], Find(query));
    }

    [Fact]
    public void FolderNamesAsRegex_StartFolderPathNotMatched()
    {
        SearchQuery query = Query("*.cs");
        query.FolderNamesAsRegex = true;
        query.RegularExpressionOn = RegexTarget.PathAndFileName;
        query.FolderNamePattern = Path.GetFileName(_root);

        Assert.Empty(Find(query));
    }

    [Fact]
    public void BaseNameAsRegex_OnlyFilesOfMatchingFoldersAllWalked()
    {
        SearchQuery query = Query("*.cs");
        query.BaseNameAsRegex = true;
        query.BaseNamePattern = "^(src|TESTS)$";

        Assert.Equal([@"src\x.cs", @"src\tests\t.cs"], Find(query));
    }

    [Fact]
    public void SeveralFolders_QuotedWithBlanksEachFileOnceMissingNoted()
    {
        FileFinder finder = new();
        SearchQuery query = Query("*.cs");
        query.FolderName = $"\"{Path.Combine(_root, "my folder")}\" \"{Path.Combine(_root, "other")}\" "
            + $"\"{Path.Combine(_root, "src")}\" \"{_root}\" \"{Path.Combine(_root, "missing")}\"";

        List<string> files = [.. finder.Find(query, CancellationToken.None).Select(Relative)];

        Assert.Equal([@"my folder\m.cs", @"other\o.cs", @"src\x.cs", @"src\bin\out.cs", @"src\tests\t.cs", "a.cs", @".git\g.cs"], files);
        Assert.Equal($"Folder not found: '{Path.Combine(_root, "missing")}'", Assert.Single(finder.Problems));
    }

    [Fact]
    public void InvalidRegex_FinderThrowsWithInputNameEngineReportsProblem()
    {
        SearchQuery query = Query("(");
        query.FileNamesAsRegex = true;

        ArgumentException ex = Assert.ThrowsAny<ArgumentException>(() => new FileFinder().Find(query, CancellationToken.None));
        Assert.StartsWith("File names:", ex.Message);

        query.SearchString = "x";
        SearchResult result = SearchEngine.Run(query, null, CancellationToken.None);
        Assert.StartsWith("Invalid regular expression: File names:", Assert.Single(result.Problems));
        Assert.Equal(0, result.FilesSearched);
    }

    [Fact]
    public void RegexProblems_OnlyOptionsSwitchedOn()
    {
        SearchQuery query = new()
        {
            SearchString = "(", FileNames = "(", FolderNamePattern = "[", BaseNamePattern = "(",
            FolderNamesAsRegex = true, BaseNameAsRegex = true,
        };

        IReadOnlyList<string> problems = FileFilter.RegexProblems(query);

        Assert.Equal(2, problems.Count);
        Assert.StartsWith("Folder names:", problems[0]);
        Assert.StartsWith("Base name:", problems[1]);

        query.RegularExpression = true;
        query.FileNamesAsRegex = true;
        Assert.Equal(4, FileFilter.RegexProblems(query).Count);
    }

    private SearchQuery Query(string fileNames = "*.*") =>
        new() { SearchString = "x", FileNames = fileNames, FolderName = $"\"{_root}\"" };

    private List<string> Find(SearchQuery query) => [.. new FileFinder().Find(query, CancellationToken.None).Select(Relative)];

    private string Relative(string path) => Path.GetRelativePath(_root, path);
}
