using RecentLists;
using SaveAndLoad;

namespace RecentLists.Tests;

/// <summary>Example pinned list, ignoring case.</summary>
public sealed class TestPinnedStrings : PinnedRecentList<string>, IStorable
{
    public static string FileName => "TestPinnedStrings.json";

    public TestPinnedStrings() : base(maxCount: 5, maxFixedCount: 3, comparer: StringComparer.OrdinalIgnoreCase) { }
}

/// <summary>Old file format: only used items.</summary>
public sealed class TestPlainStrings : RecentList<string>, IStorable
{
    public static string FileName => "TestPinnedStrings.json";

    public TestPlainStrings() : base(maxCount: 5) { }
}

public sealed class PinnedRecentListTests : IDisposable
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
    public void Add_ChangesOnlyUsedItems_FixedStayInPlace()
    {
        TestPinnedStrings list = new() { Fixed = ["f1", "f2"] };

        list.Add("a");
        list.Add("f2");

        Assert.Equal(["f2", "a"], list.Items);
        Assert.Equal(["f1", "f2"], list.Fixed);
    }

    [Fact]
    public void Merged_PreUsedThenFirstFixedThenRestUsedThenRestFixed()
    {
        TestPinnedStrings list = new() { Items = ["u1", "u2", "u3", "u4"], Fixed = ["f1", "f2", "f3"] };

        IReadOnlyList<string> merged = list.Merged(preFixedCount: 2, firstFixedCount: 2);

        Assert.Equal(["u1", "u2", "f1", "f2", "u3", "u4", "f3"], merged);
    }

    [Fact]
    public void Merged_ItemInBothLists_OnlyOnce()
    {
        TestPinnedStrings list = new() { Items = ["u1", "F1", "u2"], Fixed = ["f1", "f2"] };

        IReadOnlyList<string> merged = list.Merged(preFixedCount: 1, firstFixedCount: 1);

        Assert.Equal(["u1", "f1", "u2", "f2"], merged);
    }

    [Fact]
    public void Merged_ZeroPreFixed_FixedFirst()
    {
        TestPinnedStrings list = new() { Items = ["u1"], Fixed = ["f1"] };

        Assert.Equal(["f1", "u1"], list.Merged(preFixedCount: 0, firstFixedCount: 5));
    }

    [Fact]
    public void IsFixed_UsesComparer()
    {
        TestPinnedStrings list = new() { Fixed = ["Foo"] };

        Assert.True(list.IsFixed("foo"));
        Assert.False(list.IsFixed("bar"));
    }

    [Fact]
    public void MaxFixedCount_Smaller_RemovesLastFixed()
    {
        TestPinnedStrings list = new() { Fixed = ["f1", "f2", "f3"] };

        list.MaxFixedCount = 1;

        Assert.Equal(["f1"], list.Fixed);
    }

    [Fact]
    public void MaxFixedCount_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestPinnedStrings().MaxFixedCount = -1);
    }

    [Fact]
    public void SaveAndLoad_KeepsBothLists()
    {
        ObjectStore store = Store();
        TestPinnedStrings list = new() { Fixed = ["f1"] };
        list.Add("u1");

        store.Save(list);
        TestPinnedStrings loaded = store.Load<TestPinnedStrings>().Value;

        Assert.Equal(["u1"], loaded.Items);
        Assert.Equal(["f1"], loaded.Fixed);
    }

    [Fact]
    public void Load_OldFileWithoutFixed_EmptyFixedList()
    {
        ObjectStore store = Store();
        TestPlainStrings old = new();
        old.Add("u1");
        store.Save(old);

        LoadResult<TestPinnedStrings> loaded = store.Load<TestPinnedStrings>();

        Assert.Null(loaded.Problem);
        Assert.Equal(["u1"], loaded.Value.Items);
        Assert.Empty(loaded.Value.Fixed);
    }

    [Fact]
    public void EditRows_DropdownOrder_UsedItemThatIsFixedShownOnceAsFixed()
    {
        TestPinnedStrings list = new() { Items = ["u1", "F1", "u2"], Fixed = ["f1", "f2"] };

        IReadOnlyList<PinnedRow<string>> rows = list.EditRows(preFixedCount: 1, firstFixedCount: 1);

        Assert.Equal(
            [new("u1", false), new("f1", true), new("u2", false), new("f2", true)],
            rows);
    }

    [Fact]
    public void Arrange_PreFixedUsedRowsAlwaysFirst_GroupsKeepTheirOrder()
    {
        PinnedRow<string>[] rows =
        [
            new("f1", true), new("u1", false), new("f2", true), new("u2", false), new("u3", false),
        ];

        IReadOnlyList<PinnedRow<string>> arranged = TestPinnedStrings.Arrange(rows, 2, 1, StringComparer.Ordinal);

        Assert.Equal(
            [new("u1", false), new("u2", false), new("f1", true), new("u3", false), new("f2", true)],
            arranged);
    }

    [Fact]
    public void Arrange_NewlyTickedRow_MovesToFixedGroup()
    {
        PinnedRow<string>[] rows = [new("u1", true), new("u2", false), new("f1", true)];

        IReadOnlyList<PinnedRow<string>> arranged = TestPinnedStrings.Arrange(rows, 1, 5, StringComparer.Ordinal);

        Assert.Equal([new("u2", false), new("u1", true), new("f1", true)], arranged);
    }

    [Fact]
    public void ReplaceAll_SplitsRowsKeepsOrderAndFirstOfDuplicates()
    {
        TestPinnedStrings list = new() { Items = ["old"], Fixed = ["oldFixed"] };

        list.ReplaceAll([new("u1", false), new("f1", true), new("U1", true), new("u2", false)]);

        Assert.Equal(["u1", "u2"], list.Items);
        Assert.Equal(["f1"], list.Fixed);
    }

    [Fact]
    public void ReplaceAll_AppliesLimits()
    {
        TestPinnedStrings list = new();

        list.ReplaceAll(Enumerable.Range(1, 8).Select(i => new PinnedRow<string>($"u{i}", false))
            .Concat(Enumerable.Range(1, 5).Select(i => new PinnedRow<string>($"f{i}", true))));

        Assert.Equal(5, list.Items.Count);
        Assert.Equal(["f1", "f2", "f3"], list.Fixed);
    }

    private ObjectStore Store() => new(new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "Defaults")));
}
