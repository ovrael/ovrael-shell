namespace OvraelShell.Utils;

/// <summary>Compares lists by their items - a list read again with the same items is equal.</summary>
public sealed class SequenceComparer<T> : IEqualityComparer<IReadOnlyList<T>>
{
    public static SequenceComparer<T> Instance { get; } = new();

    public bool Equals(IReadOnlyList<T>? x, IReadOnlyList<T>? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null && x.SequenceEqual(y));

    public int GetHashCode(IReadOnlyList<T> list)
    {
        var hash = new HashCode();
        foreach (var item in list)
            hash.Add(item);

        return hash.ToHashCode();
    }
}
