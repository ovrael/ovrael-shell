namespace OvraelShell.Models;

public sealed class ReactiveCollection<T>
{
    private readonly List<T> _items = [];

    public IReadOnlyList<T> Items => _items;

    public int Count => _items.Count;

    public event Action? Changed;
    public event Action? Cleared;

    public event Action<T>? Added;

    public event Action<T>? Removed;

    public void Add(T item)
    {
        _items.Add(item);

        Added?.Invoke(item);
        Changed?.Invoke();
    }

    public bool Remove(T item)
    {
        if (!_items.Remove(item))
            return false;

        Removed?.Invoke(item);
        Changed?.Invoke();

        return true;
    }

    public void RemoveWhere(Func<T, bool> predicate)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (!predicate(_items[i]))
                continue;

            var item = _items[i];
            _items.RemoveAt(i);

            Removed?.Invoke(item);
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        if (_items.Count == 0)
            return;

        _items.Clear();
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

        foreach (var item in _items.ToArray())
        {
            if (wanted.Contains(keyOf(item)))
                continue;

            Remove(item);
            onRemoved?.Invoke(item);
        }

        var existing = _items.Select(keyOf).ToHashSet();

        foreach (var key in wanted)
        {
            if (existing.Contains(key))
                continue;

            if (create(key) is { } item)
                Add(item);
        }
    }

    public T this[int index] => _items[index];
}
