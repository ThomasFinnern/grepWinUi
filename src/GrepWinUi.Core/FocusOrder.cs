namespace GrepWinUi.Core;

/// <summary>Tab order helpers for forms that need their own order (e.g. main inputs first).</summary>
public static class FocusOrder
{
    /// <summary>
    /// <paramref name="first"/> in their given order, then the other items of <paramref name="natural"/>
    /// in their natural order.
    /// </summary>
    public static IReadOnlyList<T> FirstThenRest<T>(IEnumerable<T> natural, IReadOnlyList<T> first)
    {
        List<T> order = [.. first];
        order.AddRange(natural.Where(item => !first.Contains(item)));
        return order;
    }

    /// <summary>
    /// Index of the next item (wraps around). <paramref name="current"/> = -1 (nothing focused)
    /// gives the first item forward and the last one backward.
    /// </summary>
    public static int NextIndex(int count, int current, bool forward)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        if (current < 0)
        {
            return forward ? 0 : count - 1;
        }
        return forward ? (current + 1) % count : (current - 1 + count) % count;
    }
}
