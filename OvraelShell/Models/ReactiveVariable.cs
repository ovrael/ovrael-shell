namespace OvraelShell.Models;

public sealed class ReactiveProperty<T>
{
    private T value;
    private readonly IEqualityComparer<T> comparer;

    public T Value => value;

    public event Action<T>? Changed;

    /// <param name="comparer">Decides when a new value is a change. Default equality if null.</param>
    public ReactiveProperty(T initialValue = default!, IEqualityComparer<T>? comparer = null)
    {
        value = initialValue;
        this.comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public bool Set(T value)
    {
        if (comparer.Equals(this.value, value))
            return false;

        this.value = value;
        Changed?.Invoke(value);

        return true;
    }

    public static implicit operator T(ReactiveProperty<T> property) => property.Value;

    public override string ToString() => value?.ToString() ?? string.Empty;
}
