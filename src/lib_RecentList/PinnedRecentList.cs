using System.Text.Json.Serialization;
using SaveAndLoad;

namespace RecentLists;

/// <summary>One row of an edit dialog: an item and whether it is fixed.</summary>
/// <typeparam name="TItem">Type of the list items.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="IsFixed">True for a fixed (pinned) item.</param>
public sealed record PinnedRow<TItem>(TItem Item, bool IsFixed);

/// <summary>
/// Recent-first list with a second list of fixed (pinned) items.
/// <see cref="RecentList{TItem}.Add"/> only changes the used items (<see cref="StoredList{TItem}.Items"/>);
/// fixed items never move or drop out by adding. <see cref="Merged"/> builds the order of a dropdown.
/// Both lists are stored; the limits are set at runtime (e.g. from settings).
/// </summary>
/// <typeparam name="TItem">Type of the list items.</typeparam>
public abstract class PinnedRecentList<TItem> : RecentList<TItem>
{
    /// <summary>Standard number of kept fixed items.</summary>
    public const int DefaultMaxFixedCount = 15;

    private int _maxFixedCount;

    /// <summary>Creates an empty list.</summary>
    /// <param name="maxCount">Number of kept used items (at least 1).</param>
    /// <param name="maxFixedCount">Number of kept fixed items (at least 0).</param>
    /// <param name="comparer">Decides if two items are the same; default: Equals.</param>
    protected PinnedRecentList(int maxCount = DefaultMaxCount, int maxFixedCount = DefaultMaxFixedCount, IEqualityComparer<TItem>? comparer = null)
        : base(maxCount, comparer)
    {
        MaxFixedCount = maxFixedCount;
    }

    /// <summary>Fixed (pinned) items in their own order.</summary>
    public List<TItem> Fixed { get; set; } = [];

    /// <summary>
    /// Number of kept fixed items (at least 0). Can be changed at runtime; a smaller value removes the
    /// last fixed items at once. Not stored in the file.
    /// </summary>
    [JsonIgnore]
    public int MaxFixedCount
    {
        get => _maxFixedCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _maxFixedCount = value;
            TrimFixedToMaxCount();
        }
    }

    /// <summary>True when an equal item is fixed.</summary>
    public bool IsFixed(TItem item) => Fixed.Exists(existing => Comparer.Equals(existing, item));

    /// <summary>Removes the last fixed items beyond <see cref="MaxFixedCount"/> (e.g. after loading a longer file).</summary>
    public void TrimFixedToMaxCount()
    {
        if (Fixed.Count > MaxFixedCount)
        {
            Fixed.RemoveRange(MaxFixedCount, Fixed.Count - MaxFixedCount);
        }
    }

    /// <summary>
    /// Rows for an edit dialog in dropdown order (<see cref="Arrange"/>): each item once, marked fixed
    /// when it is in <see cref="Fixed"/>.
    /// </summary>
    public IReadOnlyList<PinnedRow<TItem>> EditRows(int preFixedCount, int firstFixedCount)
    {
        List<PinnedRow<TItem>> rows = [.. Fixed.Select(item => new PinnedRow<TItem>(item, IsFixed: true))];
        rows.AddRange(Items.Where(item => !IsFixed(item)).Select(item => new PinnedRow<TItem>(item, IsFixed: false)));
        return Arrange(rows, preFixedCount, firstFixedCount, Comparer);
    }

    /// <summary>
    /// Dropdown order of rows: the first <paramref name="preFixedCount"/> used rows, then up to
    /// <paramref name="firstFixedCount"/> fixed rows, then the other used rows, then the other fixed rows.
    /// Used and fixed rows each keep their order; an item only once (first row wins).
    /// </summary>
    public static IReadOnlyList<PinnedRow<TItem>> Arrange(
        IEnumerable<PinnedRow<TItem>> rows, int preFixedCount, int firstFixedCount, IEqualityComparer<TItem> comparer)
    {
        List<PinnedRow<TItem>> all = [.. rows];
        List<PinnedRow<TItem>> used = all.FindAll(row => !row.IsFixed);
        List<PinnedRow<TItem>> pinned = all.FindAll(row => row.IsFixed);
        List<PinnedRow<TItem>> result = [];
        int usedIndex = 0;
        int fixedIndex = 0;
        Take(used, ref usedIndex, preFixedCount);
        Take(pinned, ref fixedIndex, firstFixedCount);
        Take(used, ref usedIndex, int.MaxValue);
        Take(pinned, ref fixedIndex, int.MaxValue);
        return result;

        void Take(List<PinnedRow<TItem>> source, ref int index, int count)
        {
            while (index < source.Count && count > 0)
            {
                PinnedRow<TItem> row = source[index++];
                if (!result.Exists(existing => comparer.Equals(existing.Item, row.Item)))
                {
                    result.Add(row);
                    count--;
                }
            }
        }
    }

    /// <summary>
    /// Replaces both lists with edited rows: fixed rows become <see cref="Fixed"/> and the others the used
    /// items, each in row order, each item only once. Limits apply (the last ones drop out).
    /// </summary>
    public void ReplaceAll(IEnumerable<PinnedRow<TItem>> rows)
    {
        Fixed.Clear();
        Items.Clear();
        foreach (PinnedRow<TItem> row in rows)
        {
            List<TItem> target = row.IsFixed ? Fixed : Items;
            if (!IsFixed(row.Item) && !Items.Exists(existing => Comparer.Equals(existing, row.Item)))
            {
                target.Add(row.Item);
            }
        }
        TrimToMaxCount();
        TrimFixedToMaxCount();
    }

    /// <summary>
    /// Dropdown order: the first <paramref name="preFixedCount"/> used items, then up to
    /// <paramref name="firstFixedCount"/> fixed items, then the other used items, then the other fixed
    /// items. Each item only once.
    /// </summary>
    public IReadOnlyList<TItem> Merged(int preFixedCount, int firstFixedCount)
    {
        IEnumerable<PinnedRow<TItem>> rows = Items.Select(item => new PinnedRow<TItem>(item, IsFixed: false))
            .Concat(Fixed.Select(item => new PinnedRow<TItem>(item, IsFixed: true)));
        return [.. Arrange(rows, preFixedCount, firstFixedCount, Comparer).Select(row => row.Item)];
    }
}
