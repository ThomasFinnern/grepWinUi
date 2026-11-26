using GrepWinUi.Core.AppData.Search;
using GrepWinUi.Core.Search;

namespace GrepWinUi.Core.Tests;

/// <summary>Filter by file date (temp folders only for the search test).</summary>
public sealed class FileDateFilterTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 3, 31, 12, 0, 0);
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Theory]
    [InlineData(30, TimeUnit.Seconds, "2026-03-31 11:59:30")]
    [InlineData(5, TimeUnit.Minutes, "2026-03-31 11:55:00")]
    [InlineData(2, TimeUnit.Hours, "2026-03-31 10:00:00")]
    [InlineData(3, TimeUnit.Days, "2026-03-28 12:00:00")]
    [InlineData(2, TimeUnit.Weeks, "2026-03-17 12:00:00")]
    [InlineData(1, TimeUnit.Months, "2026-02-28 12:00:00")]
    [InlineData(2, TimeUnit.Years, "2024-03-31 12:00:00")]
    public void Subtract_Units(int count, TimeUnit unit, string expected)
    {
        Assert.Equal(DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), FileDateFilter.Subtract(Now, count, unit));
    }

    [Fact]
    public void Subtract_BeforeYearOne_MinValue()
    {
        Assert.Equal(DateTime.MinValue, FileDateFilter.Subtract(Now, 9999, TimeUnit.Years));
    }

    [Fact]
    public void Off_OrNoSubOption_AcceptsAll()
    {
        SearchQuery query = new() { ModifiedWithinLast = true, ModifiedWithinValue = 1, ModifiedWithinUnit = TimeUnit.Days };
        Assert.False(FileDateFilter.Create(query, Now).IsActive);

        query.FilterByFileDate = true;
        query.ModifiedWithinLast = false;
        Assert.False(FileDateFilter.Create(query, Now).IsActive);
        Assert.True(FileDateFilter.Create(query, Now).Accepts(DateTime.MinValue));
    }

    [Fact]
    public void WithinLast_FromStartOfSearch()
    {
        FileDateFilter filter = FileDateFilter.Create(Query(within: (3, TimeUnit.Days)), Now);

        Assert.True(filter.Accepts(Now.AddDays(-3)));
        Assert.True(filter.Accepts(Now));
        Assert.False(filter.Accepts(Now.AddDays(-3).AddSeconds(-1)));
    }

    [Fact]
    public void Between_BothDaysIncluded()
    {
        FileDateFilter filter = FileDateFilter.Create(Query(between: (new DateTime(2026, 3, 1), new DateTime(2026, 3, 10))), Now);

        Assert.True(filter.Accepts(new DateTime(2026, 3, 1, 0, 0, 0)));
        Assert.True(filter.Accepts(new DateTime(2026, 3, 10, 23, 59, 59)));
        Assert.False(filter.Accepts(new DateTime(2026, 2, 28, 23, 59, 59)));
        Assert.False(filter.Accepts(new DateTime(2026, 3, 11, 0, 0, 0)));
    }

    [Fact]
    public void WithinAndBetween_BothMustBeMet()
    {
        FileDateFilter filter = FileDateFilter.Create(
            Query(within: (25, TimeUnit.Days), between: (new DateTime(2026, 3, 1), new DateTime(2026, 3, 10))), Now);

        Assert.False(filter.Accepts(new DateTime(2026, 3, 2)));
        Assert.True(filter.Accepts(new DateTime(2026, 3, 7)));
        Assert.False(filter.Accepts(new DateTime(2026, 3, 20)));
    }

    [Fact]
    public void Problem_StartAfterEnd()
    {
        SearchQuery query = Query(between: (new DateTime(2026, 3, 10), new DateTime(2026, 3, 1)));

        Assert.NotNull(FileDateFilter.Problem(query));
        Assert.Contains(FileFilter.StartProblems(query), problem => problem.Contains("Modified between"));

        query.FilterByFileDate = false;
        Assert.Null(FileDateFilter.Problem(query));
    }

    [Fact]
    public void Search_OnlyFilesModifiedWithinLast()
    {
        Directory.CreateDirectory(_root);
        string fresh = Path.Combine(_root, "fresh.txt");
        string old = Path.Combine(_root, "old.txt");
        File.WriteAllText(fresh, "foo");
        File.WriteAllText(old, "foo");
        File.SetLastWriteTime(old, DateTime.Now.AddDays(-10));
        SearchQuery query = Query(within: (2, TimeUnit.Days));
        query.SearchString = "foo";
        query.FileNames = "*.txt";
        query.FolderName = $"\"{_root}\"";

        SearchResult result = SearchEngine.Run(query, null, CancellationToken.None);

        Assert.Equal(["fresh.txt"], result.Files.Select(file => Path.GetFileName(file.Path)));
    }

    private static SearchQuery Query((int Count, TimeUnit Unit)? within = null, (DateTime From, DateTime To)? between = null)
    {
        SearchQuery query = new() { FilterByFileDate = true };
        if (within is (int count, TimeUnit unit))
        {
            query.ModifiedWithinLast = true;
            query.ModifiedWithinValue = count;
            query.ModifiedWithinUnit = unit;
        }
        if (between is (DateTime from, DateTime to))
        {
            query.ModifiedBetween = true;
            query.ModifiedFrom = from;
            query.ModifiedTo = to;
        }
        return query;
    }
}
