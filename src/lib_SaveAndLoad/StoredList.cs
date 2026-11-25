namespace SaveAndLoad;

/// <summary>Base for stored lists. Derived classes only add their file name (via <see cref="IStorable"/>).</summary>
/// <typeparam name="TItem">Type of the list items.</typeparam>
public abstract class StoredList<TItem>
{
    /// <summary>The list content.</summary>
    public List<TItem> Items { get; set; } = [];
}
