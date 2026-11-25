using RecentLists;
using SaveAndLoad;

namespace RecentLists.Tests;

/// <summary>Example list with the standard count.</summary>
public sealed class TestRecentStrings : RecentList<string>, IStorable
{
    public static string FileName => "TestRecentStrings.json";

    public TestRecentStrings() : base(comparer: StringComparer.Ordinal) { }
}

/// <summary>Example list with own count, ignoring case.</summary>
public sealed class TestRecentFolders : RecentList<string>, IStorable
{
    public static string FileName => "TestRecentFolders.json";

    public TestRecentFolders() : base(maxCount: 3, comparer: StringComparer.OrdinalIgnoreCase) { }
}

public sealed class RecentListTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lib_RecentList.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void NewList_HasStandardCount15()
    {
        Assert.Equal(15, new TestRecentStrings().MaxCount);
    }

    [Fact]
    public void Add_NewItem_GoesOnTop()
    {
        TestRecentStrings list = new();

        list.Add("a");
        list.Add("b");

        Assert.Equal(["b", "a"], list.Items);
    }

    [Fact]
    public void Add_ExistingItem_MovesToTopWithoutDuplicate()
    {
        TestRecentStrings list = new();
        list.Add("a");
        list.Add("b");
        list.Add("c");

        list.Add("a");

        Assert.Equal(["a", "c", "b"], list.Items);
    }

    [Fact]
    public void Add_BeyondStandardCount_DropsOldest()
    {
        TestRecentStrings list = new();
        for (int i = 1; i <= 16; i++)
        {
            list.Add(i.ToString());
        }

        Assert.Equal(15, list.Items.Count);
        Assert.Equal("16", list.Items[0]);
        Assert.DoesNotContain("1", list.Items);
    }

    [Fact]
    public void Add_BeyondOwnCount_DropsOldest()
    {
        TestRecentFolders list = new();
        list.Add("a");
        list.Add("b");
        list.Add("c");
        list.Add("d");

        Assert.Equal(["d", "c", "b"], list.Items);
    }

    [Fact]
    public void Add_UsesComparer_IgnoringCase()
    {
        TestRecentFolders list = new();
        list.Add(@"C:\Temp");
        list.Add(@"D:\Data");

        list.Add(@"c:\temp");

        Assert.Equal([@"c:\temp", @"D:\Data"], list.Items);
    }

    [Fact]
    public void MaxCount_Smaller_RemovesOldestAtOnce()
    {
        TestRecentStrings list = new();
        list.Add("a");
        list.Add("b");
        list.Add("c");

        list.MaxCount = 2;

        Assert.Equal(["c", "b"], list.Items);
    }

    [Fact]
    public void MaxCount_Larger_KeepsMoreItems()
    {
        TestRecentFolders list = new() { MaxCount = 5 };
        foreach (string item in new[] { "a", "b", "c", "d", "e" })
        {
            list.Add(item);
        }

        Assert.Equal(5, list.Items.Count);
    }

    [Fact]
    public void MaxCount_BelowOne_IsRejected()
    {
        TestRecentStrings list = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => list.MaxCount = 0);
    }

    [Fact]
    public void TrimToMaxCount_AfterLongerItems_KeepsNewest()
    {
        TestRecentFolders list = new() { Items = ["a", "b", "c", "d", "e"] };

        list.TrimToMaxCount();

        Assert.Equal(["a", "b", "c"], list.Items);
    }

    [Fact]
    public void SaveThenLoad_KeepsOrder_AndMaxCountIsNotInFile()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "NoDefaults")));
        TestRecentStrings saved = new();
        saved.Add("a");
        saved.Add("b");
        saved.MaxCount = 7;

        store.Save(saved);
        TestRecentStrings loaded = store.Load<TestRecentStrings>().Value;

        Assert.Equal(["b", "a"], loaded.Items);
        Assert.Equal(15, loaded.MaxCount);
        Assert.DoesNotContain("MaxCount", File.ReadAllText(store.UserFilePath<TestRecentStrings>()));
    }
}
