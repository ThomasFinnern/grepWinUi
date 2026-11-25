using SaveAndLoad;

namespace SaveAndLoad.Tests;

/// <summary>Example storable class: only content items and file name.</summary>
public sealed class TestSettings : IStorable
{
    public static string FileName => "TestSettings.json";

    public string Name { get; set; } = "built-in";
    public int Count { get; set; } = 1;
}

/// <summary>Example stored list.</summary>
public sealed class TestList : StoredList<string>, IStorable
{
    public static string FileName => "TestList.json";
}

public sealed class ObjectStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lib_SaveAndLoad.Tests", Guid.NewGuid().ToString("N"));
    private readonly StorageFolders _folders;
    private readonly ObjectStore _store;

    public ObjectStoreTests()
    {
        _folders = new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "Defaults"));
        _store = new ObjectStore(_folders);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private void WriteDefaultsFile(string fileName, string content)
    {
        Directory.CreateDirectory(_folders.DefaultsFolder);
        File.WriteAllText(Path.Combine(_folders.DefaultsFolder, fileName), content);
    }

    private void WriteUserFile(string fileName, string content)
    {
        Directory.CreateDirectory(_folders.UserFolder);
        File.WriteAllText(Path.Combine(_folders.UserFolder, fileName), content);
    }

    [Fact]
    public void Load_NoFiles_ReturnsNewObjectWithBuiltInValues()
    {
        LoadResult<TestSettings> result = _store.Load<TestSettings>();

        Assert.Equal(LoadSource.NewObject, result.Source);
        Assert.Equal("built-in", result.Value.Name);
        Assert.Null(result.Problem);
    }

    [Fact]
    public void SaveThenLoad_ReturnsSavedValuesFromUserFile()
    {
        _store.Save(new TestSettings { Name = "saved", Count = 7 });

        LoadResult<TestSettings> result = _store.Load<TestSettings>();

        Assert.Equal(LoadSource.UserFile, result.Source);
        Assert.Equal("saved", result.Value.Name);
        Assert.Equal(7, result.Value.Count);
    }

    [Fact]
    public void Load_OnlyDefaultsFile_ReturnsStandardData()
    {
        WriteDefaultsFile(TestSettings.FileName, """{ "Name": "standard", "Count": 3 }""");

        LoadResult<TestSettings> result = _store.Load<TestSettings>();

        Assert.Equal(LoadSource.DefaultsFile, result.Source);
        Assert.Equal("standard", result.Value.Name);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public void Load_UserFileWins_OverDefaultsFile()
    {
        WriteDefaultsFile(TestSettings.FileName, """{ "Name": "standard" }""");
        WriteUserFile(TestSettings.FileName, """{ "Name": "user" }""");

        LoadResult<TestSettings> result = _store.Load<TestSettings>();

        Assert.Equal(LoadSource.UserFile, result.Source);
        Assert.Equal("user", result.Value.Name);
    }

    [Fact]
    public void Load_BrokenUserFile_FallsBackToDefaultsAndReportsProblem()
    {
        WriteDefaultsFile(TestSettings.FileName, """{ "Name": "standard" }""");
        WriteUserFile(TestSettings.FileName, "this is not json");

        LoadResult<TestSettings> result = _store.Load<TestSettings>();

        Assert.Equal(LoadSource.DefaultsFile, result.Source);
        Assert.Equal("standard", result.Value.Name);
        Assert.NotNull(result.Problem);
    }

    [Fact]
    public void Load_BrokenUserFileAndNoDefaults_ReturnsNewObjectAndReportsProblem()
    {
        WriteUserFile(TestSettings.FileName, "{ broken");

        LoadResult<TestSettings> result = _store.Load<TestSettings>();

        Assert.Equal(LoadSource.NewObject, result.Source);
        Assert.Equal("built-in", result.Value.Name);
        Assert.NotNull(result.Problem);
    }

    [Fact]
    public void Save_CreatesMissingUserFolder_AndLeavesNoTempFile()
    {
        Assert.False(Directory.Exists(_folders.UserFolder));

        _store.Save(new TestSettings());

        Assert.True(File.Exists(_store.UserFilePath<TestSettings>()));
        Assert.Empty(Directory.GetFiles(_folders.UserFolder, "*.tmp"));
    }

    [Fact]
    public void Save_OverwritesExistingUserFile()
    {
        _store.Save(new TestSettings { Name = "first" });
        _store.Save(new TestSettings { Name = "second" });

        Assert.Equal("second", _store.Load<TestSettings>().Value.Name);
    }

    [Fact]
    public void Load_DoesNotWriteUserFile()
    {
        WriteDefaultsFile(TestSettings.FileName, """{ "Name": "standard" }""");

        _store.Load<TestSettings>();

        Assert.False(File.Exists(_store.UserFilePath<TestSettings>()));
    }

    [Fact]
    public void StoredList_RoundTrip_KeepsItemOrder()
    {
        TestList list = new() { Items = ["c", "a", "b"] };

        _store.Save(list);
        LoadResult<TestList> result = _store.Load<TestList>();

        Assert.Equal(["c", "a", "b"], result.Value.Items);
    }
}
