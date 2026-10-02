namespace OvraelShell.Models;

public sealed class ReactiveCollection<T>
{
    private readonly List<T> items = [];

    public IReadOnlyList<T> Items => items;

    public int Count => items.Count;

    public event Action? Changed;
    public event Action? Cleared;

    public event Action<T>? Added;

    public event Action<T>? Removed;

    public void Add(T item)
    {
        items.Add(item);

        Added?.Invoke(item);
        Changed?.Invoke();
    }

    public bool Remove(T item)
    {
        if (!items.Remove(item))
            return false;

        Removed?.Invoke(item);
        Changed?.Invoke();

        return true;
    }

    public bool RemoveWhere(Func<T, bool> predicate)
    {
        bool removed = false;
        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (!predicate(items[i]))
                continue;

            var item = items[i];
            items.RemoveAt(i);

            removed = true;
            Removed?.Invoke(item);
        }

        if (removed)
            Changed?.Invoke();

        return removed;
    }

    public void Clear()
    {
        if (items.Count == 0)
            return;

        items.Clear();
        Cleared?.Invoke();
        Changed?.Invoke();
    }

    /// <summary>
    /// Syncs the collection with a list of keys: removes items whose key is missing
    /// and creates the missing ones. <paramref name="create"/> may return null to skip a key.
    /// </summary>
    public void Sync<TKey>(
        IEnumerable<TKey> keys,
        Func<T, TKey> keyOf,
        Func<TKey, T?> create,
        Action<T>? onRemoved = null
    )
    {
        var wanted = keys.ToHashSet();

        foreach (var item in items.ToArray())
        {
            if (wanted.Contains(keyOf(item)))
                continue;

            Remove(item);
            onRemoved?.Invoke(item);
        }

        var existing = items.Select(keyOf).ToHashSet();

        foreach (var key in wanted)
        {
            if (existing.Contains(key))
                continue;

            if (create(key) is { } item)
                Add(item);
        }
    }

    public T this[int index] => items[index];
}
