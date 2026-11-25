using System.Text.Json.Serialization;
using SaveAndLoad;

namespace RecentLists;

/// <summary>
/// Stored list with the newest item on top. Adding an existing item moves it to the top.
/// Keeps at most <see cref="MaxCount"/> items; the oldest fall out.
/// </summary>
/// <typeparam name="TItem">Type of the list items.</typeparam>
public abstract class RecentList<TItem> : StoredList<TItem>
{
    /// <summary>Standard number of kept items.</summary>
    public const int DefaultMaxCount = 15;

    private readonly IEqualityComparer<TItem> _comparer;
    private int _maxCount;

    /// <summary>Creates an empty list.</summary>
    /// <param name="maxCount">Number of kept items (at least 1).</param>
    /// <param name="comparer">Decides if two items are the same; default: Equals.</param>
    protected RecentList(int maxCount = DefaultMaxCount, IEqualityComparer<TItem>? comparer = null)
    {
        MaxCount = maxCount;
        _comparer = comparer ?? EqualityComparer<TItem>.Default;
    }

    /// <summary>
    /// Number of kept items (at least 1). Can be changed at runtime, e.g. from settings;
    /// a smaller value removes the oldest items at once. Not stored in the file.
    /// </summary>
    [JsonIgnore]
    public int MaxCount
    {
        get => _maxCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxCount = value;
            TrimToMaxCount();
        }
    }

    /// <summary>Puts the item on top; an existing equal item is moved there. Removes the oldest beyond <see cref="MaxCount"/>.</summary>
    public void Add(TItem item)
    {
        int index = Items.FindIndex(existing => _comparer.Equals(existing, item));
        if (index >= 0)
        {
            Items.RemoveAt(index);
        }
        Items.Insert(0, item);
        TrimToMaxCount();
    }

    /// <summary>Removes the oldest items beyond <see cref="MaxCount"/> (e.g. after loading a longer file).</summary>
    public void TrimToMaxCount()
    {
        if (Items.Count > MaxCount)
        {
            Items.RemoveRange(MaxCount, Items.Count - MaxCount);
        }
    }
}
