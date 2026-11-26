using GrepWinUi.Core.AppData.Search;

namespace GrepWinUi.Core.Search;

/// <summary>
/// "Filter by file date" of a search query, on the file's last modified time (local time). Only with
/// <see cref="SearchQuery.FilterByFileDate"/>: "Modified within last" N units (from the search start) and
/// "Modified between" two days (both days included); both together must both be met.
/// </summary>
public sealed class FileDateFilter
{
    private readonly DateTime? _from;
    private readonly DateTime? _before;

    private FileDateFilter(DateTime? from, DateTime? before)
    {
        _from = from;
        _before = before;
    }

    /// <summary>True when the filter checks dates (else every file is accepted).</summary>
    public bool IsActive => _from is not null || _before is not null;

    /// <summary>Filter of <paramref name="query"/>; <paramref name="now"/> = start of the search.</summary>
    public static FileDateFilter Create(SearchQuery query, DateTime now)
    {
        if (!query.FilterByFileDate)
        {
            return new FileDateFilter(null, null);
        }

        DateTime? from = null;
        DateTime? before = null;
        if (query.ModifiedWithinLast)
        {
            from = Subtract(now, Math.Max(0, query.ModifiedWithinValue), query.ModifiedWithinUnit);
        }
        if (query.ModifiedBetween)
        {
            if (query.ModifiedFrom is DateTime start && (from is null || start.Date > from))
            {
                from = start.Date;
            }
            before = query.ModifiedTo?.Date.AddDays(1);
        }
        return new FileDateFilter(from, before);
    }

    /// <summary>True when a file modified at <paramref name="modified"/> is searched.</summary>
    public bool Accepts(DateTime modified) =>
        (_from is null || modified >= _from) && (_before is null || modified < _before);

    /// <summary>Start problem: "Modified between" with the start day after the end day; null when fine.</summary>
    public static string? Problem(SearchQuery query) =>
        query.FilterByFileDate && query.ModifiedBetween
            && query.ModifiedFrom is DateTime start && query.ModifiedTo is DateTime end && start.Date > end.Date
            ? $"\"Modified between\": the start day {start:d} is after the end day {end:d}."
            : null;

    /// <summary>
    /// <paramref name="time"/> minus <paramref name="count"/> units (months and years as calendar months / years);
    /// <see cref="DateTime.MinValue"/> when that is before year 1.
    /// </summary>
    public static DateTime Subtract(DateTime time, int count, TimeUnit unit)
    {
        try
        {
            return unit switch
            {
                TimeUnit.Seconds => time.AddSeconds(-count),
                TimeUnit.Minutes => time.AddMinutes(-count),
                TimeUnit.Hours => time.AddHours(-count),
                TimeUnit.Days => time.AddDays(-count),
                TimeUnit.Weeks => time.AddDays(-7.0 * count),
                TimeUnit.Months => time.AddMonths(-count),
                TimeUnit.Years => time.AddYears(-count),
                _ => time,
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }
}
